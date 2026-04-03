# EntApp.Framework — Kodlama Kuralları ve Mimari Kısıtlamalar

> **Tarih:** 2026-04-03  
> **AI Workflow dosyası:** [.agent/workflows/coding-conventions.md](file:///c:/Users/kaya/projects/EntApp.Framework/.agent/workflows/coding-conventions.md)  
> **Kaynak:** [Mimari Spesifikasyon](file:///c:/Users/kaya/projects/EntApp.Framework/docs/enterprise-framework-evaluation.md)

---

## 1. Modüller Arası İletişim — Altın Kurallar

Bir modül, başka bir modülün verisine yalnızca iki yolla erişir:

```csharp
// ✅ DOĞRU: MediatR üzerinden Query/Command
var project = await _mediator.Send(new GetProjectQuery(projectId));

// ✅ DOĞRU: Integration Event ile bildirim
await _eventBus.PublishAsync(new TicketCreatedEvent(ticket.Id));

// ❌ YANLIŞ: Başka modülün DbContext'ine direkt erişim
var project = await _projectDbContext.Projects.FindAsync(projectId);

// ❌ YANLIŞ: Başka modülün internal servisini inject etme
var result = _projectService.GetById(projectId);
```

**Neden?** İleride herhangi bir modül bağımsız servise çıkarıldığında:
- `MediatR` çağrısı → HTTP/gRPC'ye dönüşür
- Integration Event → RabbitMQ/Kafka mesajına dönüşür
- İş mantığı **değişmez**.

---

## 2. Entity Tasarım Kuralları

### 2.1. Temel Entity Hiyerarşisi

```csharp
// Tüm entity'lerin temeli
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }      // Soft delete
    public uint RowVersion { get; set; }     // Optimistic concurrency (EF xmin)
    
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

// Audit alanları gereken entity'ler için
public abstract class AuditableEntity : BaseEntity
{
    public string CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

// Multi-tenant entity'ler
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

### 2.2. Strongly Typed ID

```csharp
// ❌ YANLIŞ: Guid karışabilir
public async Task<Order> GetOrder(Guid orderId) { ... }
GetOrder(customerId); // Compile eder ama bug!

// ✅ DOĞRU: Strongly Typed ID
public readonly record struct OrderId(Guid Value);
public readonly record struct CustomerId(Guid Value);

public async Task<Order> GetOrder(OrderId orderId) { ... }
GetOrder(customerId); // COMPILE HATASI — OrderId ≠ CustomerId
```

Her modül kendi ID tiplerini `Domain/` klasöründe tanımlar:
```
Modules/CRM/CRM.Domain/
├── Ids/
│   ├── CustomerId.cs
│   ├── ContactId.cs
│   └── OpportunityId.cs
```

### 2.3. Soft Delete + Unique Constraint

```csharp
// EF Core konfigürasyonu
builder.HasIndex(e => e.TaxNumber)
    .IsUnique()
    .HasFilter("is_deleted = false");  // Sadece aktif kayıtlarda unique
```

Bu sayede silinen bir `TaxNumber` ile aynı numarada yeni kayıt oluşturulabilir.

### 2.4. Concurrency Control

```csharp
// BaseDbContext'te otomatik konfigürasyon
modelBuilder.Entity<BaseEntity>()
    .Property(e => e.RowVersion)
    .IsRowVersion();  // PostgreSQL xmin system column

// Kullanım — SaveChanges sırasında çakışma olursa DbUpdateConcurrencyException fırlatılır
```

---

## 3. CQRS Yapısı

### 3.1. Dosya Düzeni

```
Module.Application/
├── Commands/
│   ├── CreateOrder/
│   │   ├── CreateOrderCommand.cs           ← IRequest<Result<OrderId>>
│   │   ├── CreateOrderCommandHandler.cs    ← IRequestHandler<>
│   │   └── CreateOrderCommandValidator.cs  ← AbstractValidator<>
│   └── UpdateOrder/
│       └── ...
├── Queries/
│   ├── GetOrderById/
│   │   ├── GetOrderByIdQuery.cs            ← IRequest<Result<OrderDto>>
│   │   └── GetOrderByIdQueryHandler.cs
│   └── GetOrdersPaged/
│       └── ...
├── DTOs/
│   └── OrderDto.cs
├── EventHandlers/
│   └── OrderCreatedEventHandler.cs         ← Domain Event handler
├── Interfaces/
│   └── IOrderRepository.cs
└── Mappings/
    └── OrderMappingConfig.cs               ← Mapster TypeAdapterConfig
```

### 3.2. Result Pattern

```csharp
// Handler ASLA exception fırlatmaz (iş mantığı hataları için)
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderId>>
{
    public async Task<Result<OrderId>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var customer = await _repo.GetByIdAsync(request.CustomerId, ct);
        if (customer is null)
            return Result.Failure<OrderId>(Error.NotFound("Customer.NotFound", "Müşteri bulunamadı"));

        if (customer.Status == CustomerStatus.Blocked)
            return Result.Failure<OrderId>(Error.Validation("Customer.Blocked", "Müşteri bloke"));

        var order = new SalesOrder { ... };
        order.AddDomainEvent(new OrderCreatedEvent(order.Id));
        
        await _repo.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        
        return Result.Success(order.Id);
    }
}

// Controller'da
var result = await _mediator.Send(command);
return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
```

### 3.3. Pipeline Behavior Sırası

```
Request ──► [1] ValidationBehavior      (FluentValidation — Command + Query)
           ──► [2] LoggingBehavior      (Serilog — Command + Query)
              ──► [3] PerformanceBehavior (>500ms → warning log)
                 ──► [4] TransactionBehavior (sadece Command, ITransactionless hariç)
                    ──► [5] CachingBehavior   (sadece Query — ICacheableQuery<T> marker)
                       ──► Handler
                          ──► Response
```

### 3.4. Transaction Yönetimi

`TransactionBehavior` tüm Command handler'ları otomatik olarak transaction içinde çalıştırır. Handler içinde **sadece DB işi** yapılmalıdır. Yan etkiler Domain Event veya Integration Event ile tetiklenir.

#### 🔴 Handler İçinde YAPIN vs YAPMAYIN

```
┌─────────────────────────────────────────────────┐
│  Handler İçinde YAPIN (✅ Transaction içi)        │
│  • Entity oluşturma / güncelleme                 │
│  • Domain Event ekleme (AddDomainEvent)           │
│  • Repository çağrıları                           │
│  • UnitOfWork.SaveChangesAsync()                  │
├─────────────────────────────────────────────────┤
│  Handler İçinde YAPMAYIN (❌ Transaction'a sokmayın)│
│  • HTTP/gRPC dış servis çağrısı                  │
│  • Email/SMS gönderimi                            │
│  • Dosya oluşturma (PDF, Excel)                   │
│  • Uzun süren hesaplama/algoritma                  │
│  • Cache yazma                                    │
├─────────────────────────────────────────────────┤
│  Bunları NEREDE YAPIN?                            │
│  • Domain Event Handler (post-commit)              │
│  • Integration Event Consumer (ayrı process)       │
│  • Hangfire Background Job                         │
└─────────────────────────────────────────────────┘
```

#### ✅ Doğru: Thin Handler

```csharp
// Transaction ~15ms — sadece DB işi
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderId>>
{
    public async Task<Result<OrderId>> Handle(...)
    {
        var order = new SalesOrder { ... };

        // Yan etkiler event olarak kaydedilir, handler içinde çalışmaz
        order.AddDomainEvent(new OrderCreatedEvent(order.Id));

        await _repo.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(order.Id);
        // Post-commit: OrderCreatedEventHandler → email, PDF, bildirim
    }
}
```

#### ❌ Yanlış: Fat Handler

```csharp
// ❌ Transaction 3+ saniye açık kalır!
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderId>>
{
    public async Task<Result<OrderId>> Handle(...)
    {
        var order = new SalesOrder { ... };
        await _repo.AddAsync(order);              // 5ms
        await _paymentService.ChargeAsync(...);    // 2000ms 💣 dış servis!
        await _emailService.SendAsync(...);        // 800ms 💣 SMTP!
        await _unitOfWork.SaveChangesAsync();      // 10ms
    }
}
```

#### Transaction Opt-Out (ITransactionless)

Bazı Command'lar transaction gerektirmez (sadece dış servis çağrısı, cache invalidation vb.):

```csharp
// Transaction olmadan çalışsın
public class InvalidateCacheCommand : IRequest<Result>, ITransactionless { }

// Query'ler zaten transaction'a dahil değildir (TransactionBehavior otomatik atlar)
```

#### Dış Servis Sonucu Aynı Transaction'ın İçinde Gerekiyorsa

Dış servise **handler'dan önce** (controller/endpoint düzeyinde) erişin, sonucu Command'a koyun:

```csharp
// Controller/Endpoint:
var stockAvailable = await _stockService.CheckAsync(productId);  // Transaction dışı

var command = new CreateOrderCommand
{
    ProductId = productId,
    StockConfirmed = stockAvailable  // Sonucu command'a taşı
};

var result = await _mediator.Send(command);  // Handler'da sadece DB işi
```

---

## 4. Domain Events vs Integration Events

| | Domain Event | Integration Event |
|---|---|---|
| **Kapsam** | Modül-içi | Modüller-arası |
| **Transport** | MediatR `INotification` | MassTransit → RabbitMQ |
| **Transaction** | Aynı transaction (pre-commit) | Outbox → ayrı transaction (post-commit) |
| **Fail** | Rollback | Retry + dead-letter queue |
| **Tanım yeri** | `Module.Domain/Events/` | `Shared.Contracts/Events/` |

### Yaşam Döngüsü (İki Aşamalı Dispatch)

```
Handler: entity.AddDomainEvent(new OrderCreatedEvent(...))
         entity.AddDomainEvent(new OrderNotifyEvent(...))    // IPostCommitDomainEvent
  → SaveChangesAsync()
    → EF Interceptor (pre-commit):
        → IDomainEvent olanları dispatch → _mediator.Publish()
          → DomainEventHandler (aynı modül, aynı transaction)
            → Gerekirse: _eventBus.Publish(IntegrationEvent) → Outbox'a yaz
    → Commit başarılı (post-commit):
        → IPostCommitDomainEvent olanları dispatch
          → Email, bildirim, cache invalidation, dış servis
        → OutboxProcessor → RabbitMQ publish (arka plan worker)
```

| Event Tipi | Zamanlama | Kullanım |
|-----------|-----------|----------|
| `IDomainEvent` | Pre-commit (aynı tx) | Stok düşürme, bakiye güncelleme |
| `IPostCommitDomainEvent` | Post-commit (tx sonrası) | Email, bildirim, cache, loglama |
| `IIntegrationEvent` | Post-commit (Outbox) | Modüller arası bildirim |

### Idempotency

```csharp
public interface IIntegrationEvent
{
    Guid Id { get; }
    Guid IdempotencyKey { get; }  // Tekrar koruması
    DateTime OccurredAt { get; }
}

// Consumer tarafında
if (await _processedEvents.ExistsAsync(event.IdempotencyKey))
    return; // Zaten işlendi, atla
```

---

## 5. EF Core Kuralları

### 5.1. TPT Kalıtım (Table-Per-Type)

```csharp
// Framework tablosu: app.customer_base (app schema, prefix yok)
public class CustomerBase : AuditableEntity, ITenantEntity { ... }

// Proje tablosu: crm.customers (modül schema, prefix yok)
public class Customer : CustomerBase
{
    public decimal CreditLimit { get; set; }
}
```

### 5.2. BaseDbContext Standart Konfigürasyon

```csharp
// Framework tarafı
modelBuilder.Entity<CustomerBase>()
    .ToTable("customer_base", "app");  // app schema, prefix yok

// Proje tarafı
modelBuilder.Entity<Customer>()
    .ToTable("customers", "crm");      // modül schema, prefix yok
```

### 5.3. Migration Stratejisi

```bash
# Her modül kendi migration klasöründe
dotnet ef migrations add InitialCrm \
  --project src/Modules/CRM/CRM.Infrastructure \
  --startup-project src/Host/WebAPI \
  --context CrmDbContext \
  --output-dir ../../../database/Migrations/CRM
```

---

## 6. Dynamic UI Kuralları

### 6.1. Attribute = Sadece Veri + Validasyon

```csharp
// ✅ DOĞRU: Sadece veri tipi ve validasyon
[DynamicEntity("Country", menuGroup: "Tanımlar")]
public class Country : BaseEntity
{
    [DynamicField(required: true, maxLength: 3)]
    public string Code { get; set; }

    [DynamicField(required: true, searchable: true)]
    public string Name { get; set; }
}

// ❌ YANLIŞ: UI konfigürasyonu C# kodunda
[DynamicField("Ülke Kodu", order: 1, width: "sm", showInList: true)]
public string Code { get; set; }
```

### 6.2. UI Konfigürasyonu — DB veya JSON

```json
{
  "entity": "Country",
  "title": "Ülkeler",
  "icon": "globe",
  "fields": [
    { "name": "Code", "label": "Ülke Kodu", "order": 1, "width": "sm", "showInList": true },
    { "name": "Name", "label": "Ülke Adı", "order": 2, "showInList": true }
  ]
}
```

### 6.3. Fallback Mekanizması

```
1. DB/JSON konfigürasyonu (admin tarafından düzenlenmiş) → varsa kullan
2. Convention-based otomatik → property adından label, sırasından order
3. Attribute'tan gelen → [DynamicField] parametreleri
```

### 6.4. Orval vs Dynamic UI

| | Orval (OpenAPI → TS) | Dynamic UI (Metadata API) |
|---|---|---|
| **Kullanım** | Custom sayfalar (Level 4) | CRUD ekranlar (Level 1-3) |
| **Tip bilgisi** | Build-time (compile) | Runtime (metadata API) |
| **Ne üretir** | Axios hook + DTO tipleri | Otomatik tablo + form |

---

## 7. API Tasarım Kuralları

### 7.1. RFC 7807 ProblemDetails

```json
{
  "type": "https://entapp.dev/errors/validation",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "name": ["Name is required"],
    "email": ["Invalid email format"]
  }
}
```

### 7.2. Dynamic CRUD Endpoint'ler

```
GET    /api/v1/{entity}              → Listeleme (paged)
GET    /api/v1/{entity}/{id}         → Tek kayıt
POST   /api/v1/{entity}              → Oluşturma
PUT    /api/v1/{entity}/{id}         → Güncelleme
DELETE /api/v1/{entity}/{id}         → Silme (soft delete)
GET    /api/v1/{entity}/lookup       → Arama (combobox)
GET    /api/v1/{entity}/export       → Dışa aktarma
POST   /api/v1/{entity}/import       → İçe aktarma
GET    /api/v1/meta/{entity}         → Metadata (field bilgileri)
GET    /api/v1/meta/menu             → Otomatik menü
```

---

## 8. Naming Convention Tablosu

| Kavram | Format | Örnek |
|--------|--------|-------|
| Solution | `EntApp.sln` | — |
| Namespace | `EntApp.Modules.{Modül}.{Katman}` | `EntApp.Modules.CRM.Domain` |
| Entity | PascalCase, tekil | `Customer`, `SalesOrder` |
| Strongly Typed ID | `{Entity}Id` record struct | `CustomerId`, `OrderId` |
| Command | `{Fiil}{Entity}Command` | `CreateOrderCommand` |
| Query | `Get{Entity}By{Filtre}Query` | `GetOrderByIdQuery` |
| Handler | `{Request}Handler` | `CreateOrderCommandHandler` |
| Validator | `{Request}Validator` | `CreateOrderCommandValidator` |
| DTO | `{Entity}Dto` | `OrderDto`, `OrderListDto` |
| Domain Event | `{Entity}{Aksiyon}Event` | `OrderCreatedEvent` |
| Integration Event | `{Entity}{Aksiyon}IntegrationEvent` | `OrderCompletedIntegrationEvent` |
| DbContext | `{Modül}DbContext` | `CrmDbContext` |
| DB Schema | modül adı (küçük harf) | `crm`, `iam`, `hr` |
| DB Tablo (framework) | `{entity}` + `app` schema | `app.customer_base` |
| DB Tablo (proje) | `{entity}` + modül schema | `crm.customers` |
| Migration | Anlamlı isim | `AddCustomerCreditLimit` |
| Test sınıfı | `{Handler}Tests` | `CreateOrderCommandHandlerTests` |

---

## 9. Yeni Modül Ekleme Checklist

- [ ] `Modules/{Name}/` altında 4 proje oluştur (Domain, Application, Infrastructure, API)
- [ ] Strongly Typed ID'leri `Domain/Ids/` altında tanımla
- [ ] Entity'leri `BaseEntity`/`AuditableEntity` + `ITenantEntity`'den türet
- [ ] `{Name}DbContext` oluştur, kendi schema'sını kullan
- [ ] `IModuleInstaller` implement et → otomatik DI registration
- [ ] Domain Event'leri `Domain/Events/` altına koy
- [ ] Integration Event'leri `Shared.Contracts/Events/` altına koy
- [ ] `[DynamicEntity]` attribute ekle → otomatik CRUD ekranı
- [ ] `ITenantSeeder` implement et (yeni tenant oluşturulduğunda seed)
- [ ] Migration oluştur: `database/Migrations/{Name}/`
- [ ] Test projesi oluştur: `tests/Modules/{Name}.Tests/`
- [ ] Frontend route oluştur: `frontend/src/app/{name}/` (opsiyonel)

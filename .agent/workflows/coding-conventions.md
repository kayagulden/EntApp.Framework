---
description: EntApp.Framework kodlama kuralları ve mimari kısıtlamalar. Kod yazarken, düzenlerken veya yeni modül eklerken bu kurallara uyulmalıdır.
---

// turbo-all

# EntApp.Framework — Kodlama Kuralları

Bu kurallar tüm kod yazım, düzenleme ve modül oluşturma işlemlerinde geçerlidir. Detaylı açıklamalar ve örnekler için `docs/CODING_CONVENTIONS.md` dosyasına bakın.

---

## 🔴 Kesinlikle Uyulması Gereken Kurallar

### Modüller Arası İletişim
- Modüller arası iletişim **SADECE** `MediatR` (Query/Command) veya `Integration Events` ile yapılır.
- Başka modülün `DbContext`'ine direkt erişim **YASAKTIR**.
- Başka modülün internal servisini inject etme **YASAKTIR**.
- Cross-module kontratlar `Shared.Contracts/` altında tanımlanır.

### Entity Kuralları
- Tüm entity'ler `BaseEntity` veya `AuditableEntity`'den türer.
- Her entity `RowVersion` (concurrency token) taşır — `BaseEntity`'de zaten mevcut.
- Multi-tenant entity'ler `ITenantEntity` interface'ini implement eder.
- Strongly Typed ID kullanılır: `CustomerId`, `OrderId` vb. (`EntityId<T>` record struct).
- Guid dışında primitive ID kullanmayın.
- Soft delete: `IsDeleted = true` — unique constraint'ler `WHERE is_deleted = false` partial index ile tanımlanır.

### CQRS Yapısı
- Her işlem bir `Command` veya `Query` olarak tanımlanır — handler'da direkt business logic yazılmaz (Result Pattern kullanılır).
- Dosya düzeni: `Commands/CreateOrder/CreateOrderCommand.cs`, `CreateOrderCommandHandler.cs`, `CreateOrderCommandValidator.cs`
- Handler'lar **asla exception fırlatmaz** (iş mantığı hataları için). `Result<T>` döner.
- Validasyon: `FluentValidation` ile `AbstractValidator<TCommand>` — handler'dan önce `ValidationBehavior` çalışır.

### Domain Events vs Integration Events
- **Domain Event** = modül-içi, aynı transaction, `MediatR INotification`.
- **Integration Event** = modüller-arası, Outbox tablosu üzerinden, `IIntegrationEvent` + `IdempotencyKey`.
- Domain event handler **asla başka modülün DB'sine yazmaz**. Modül sınırı geçilecekse → Integration Event.
- Domain event'ler **pre-commit** dispatch edilir (aynı transaction).
- Integration event'ler **post-commit** Outbox'tan publish edilir.

### Dynamic UI Attribute Kuralları
- C# entity üzerindeki `[DynamicField]` attributeları **sadece veri tipi ve validasyon** bilgisi taşır.
- UI konfigürasyonları (label, order, width, showInList, icon) **C# kodunda TUTULMAZ** — `DynamicUIConfigs` DB tablosu veya JSON'dan yönetilir.
- UI değişikliği için backend derlemesi **GEREKMEMELİ**.

---

## 🟡 Mimari Pattern'ler

### Proje Yapısı (Her Modül)
```
Modules/{ModuleName}/
├── {ModuleName}.Domain/        → Entity, ValueObject, DomainEvent, Enum
├── {ModuleName}.Application/   → Commands/, Queries/, Handlers, DTOs/, EventHandlers/, Interfaces/
├── {ModuleName}.Infrastructure/→ EF DbContext, Repository impl, External services
└── {ModuleName}.API/           → Controller/Endpoint
```

### Naming Convention
- Solution: `EntApp.sln`
- Namespace: `EntApp.Modules.{ModuleName}.{Layer}` (ör: `EntApp.Modules.CRM.Domain`)
- Entity: PascalCase, tekil (ör: `Customer`, `SalesOrder`)
- Command: `{Verb}{Entity}Command` (ör: `CreateOrderCommand`)
- Query: `Get{Entity}By{Filter}Query` (ör: `GetOrderByIdQuery`)
- Handler: `{CommandOrQuery}Handler` (ör: `CreateOrderCommandHandler`)
- Integration Event: `{Entity}{Action}IntegrationEvent` (ör: `OrderCompletedIntegrationEvent`)
- Domain Event: `{Entity}{Action}Event` (ör: `OrderCreatedEvent`)
- DbContext: `{ModuleName}DbContext` (ör: `CrmDbContext`) — her modül kendi DbContext'i
- DB Schema: modül adı ile prefix (ör: `crm.customers`)

### EF Core Kuralları
- **TPT (Table-Per-Type)** kalıtım stratejisi — framework tabloları `app` schema'sında, proje tabloları modül schema'sında (`crm`, `hr`, `sales`). **Prefix kullanılmaz**, ayrım sadece schema ile sağlanır.
- **AsSplitQuery** varsayılan olarak açık (`BaseDbContext`'te).
- **Global Query Filter**: `TenantId`, `IsDeleted` — her sorgu otomatik filtrelenir.
- Her modülün kendi `Migrations/` klasörü vardır.
- Migration komutu: `dotnet ef migrations add X --project Module.Infrastructure --startup-project Host.WebAPI --context ModuleDbContext`

### Business Framework Kalıtımı
- Framework: `CustomerBase`, `OrderBase` vb. (generic core)
- Proje: `Customer : CustomerBase` (kuruma özel alanlar)
- Framework handler'ları `CustomerBase` ile çalışır — proje tarafında override gerekirse kendi handler'ını yazar.

### API Kuralları
- Hata response'ları **RFC 7807 ProblemDetails** standardında döner.
- API versioning: `/api/v1/{entity}` — `Asp.Versioning` kullanılır.
- Dynamic Entity'ler için: `app.MapDynamicCrudEndpoints()` tek satır ile otomatik registration.
- Rate limiting: ASP.NET Core Rate Limiter (tenant bazlı).

### Test Kuralları
- Backend: xUnit + NSubstitute + Testcontainers
- Frontend: Vitest + React Testing Library + Playwright (E2E)
- Her Command/Query handler için en az 1 unit test.
- Integration testleri `Testcontainers` ile gerçek PostgreSQL kullanır.

---

## 🟢 Frontend Kuralları

- Framework: React 19 + TypeScript + Next.js 15 (App Router)
- UI: shadcn/ui + Radix + Tailwind CSS 4
- **Custom sayfalar** (Level 4): Orval ile üretilen typed hook'lar kullanılır.
- **Dynamic UI sayfalar** (Level 1-3): Metadata API'den runtime tip alınır, `useDynamicCrud.ts` hook'u kullanılır.
- State: TanStack Query (server), Zustand (client)
- Forms: React Hook Form + Zod (metadata'dan otomatik Zod schema üretimi: `schema-to-zod.ts`)
- i18n: next-intl
- Tema: next-themes (dark/light)

### Dosya Yapısı
```
frontend/src/
├── app/           → Next.js route'ları
├── components/
│   ├── ui/        → shadcn/ui (değiştirilmez, sadece eklenir)
│   ├── dynamic/   → DynamicPage, DynamicTable, DynamicForm vb.
│   └── shared/    → Ortak bileşenler
├── features/      → Modüle özel bileşenler
├── lib/
│   ├── api/       → Axios instance, Orval generated
│   ├── hooks/     → Custom hooks
│   └── stores/    → Zustand
└── types/         → Orval generated + manual types
```

---

## 📋 Yeni Modül Ekleme Checklist

1. `Modules/{Name}/` altında 4 proje oluştur (Domain, Application, Infrastructure, API)
2. Entity'leri `BaseEntity`/`AuditableEntity` + `ITenantEntity`'den türet
3. StronglyTypedId tanımla (ör: `public readonly record struct CustomerId(Guid Value);`)
4. `{Name}DbContext` oluştur, kendi schema'sını kullan
5. `IModuleInstaller` interface'ini implement et → otomatik DI registration
6. Integration Event'leri `Shared.Contracts/Events/` altına koy
7. `[DynamicEntity]` attribute ekle → otomatik CRUD ekranı
8. `ITenantSeeder` implement et (yeni tenant oluşturulduğunda seed data)
9. Migration oluştur: kendi `Migrations/{Name}/` klasöründe
10. Unit test projesi oluştur: `Tests/Modules/{Name}.Tests/`

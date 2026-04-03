# EntApp.Framework — Business Framework Katmanı

> **Tarih:** 2026-03-28  
> **Bağımlılık:** [Mimari Spesifikasyon](file:///C:/Users/kaya/.gemini/antigravity/brain/b44af52e-bfc8-46ce-a6eb-44bfbbc2913a/enterprise-framework-evaluation.md)  
> **Durum:** Tasarım tartışması

---

## 1. Problem

Klasik framework'ler sadece **teknik altyapıyı** sağlar (CQRS, UoW, Auth, Events...). İş modülleri (CRM, Sales, HR) projeye bırakılır. Ancak bu modüllerin kurumdan kuruma değişen kısımlarının yanında **ortak çekirdeği** de vardır. Bu çekirdek her projede sıfırdan yazılır.

## 2. İki Katmanlı Framework Yapısı

```
┌─────────────────────────────────────────────────────┐
│  Proje Katmanı (kuruma özel)                        │
│  Customer : CustomerBase + ekstra alanlar            │
│  Özel iş kuralları, özel endpoint'ler               │
├─────────────────────────────────────────────────────┤
│  Business Framework (generic core)                   │
│  CustomerBase, OrderBase, EmployeeBase...            │
│  Temel CRUD, temel iş kuralları, temel API           │
├─────────────────────────────────────────────────────┤
│  Technical Framework (altyapı)                       │
│  CQRS, UoW, Events, Auth, Multi-Tenancy, Cache...   │
└─────────────────────────────────────────────────────┘
```

| Katman | Değişir mi? | Kim yazar? |
|--------|-------------|------------|
| Technical Framework | Kurumlar arası **aynı** | Framework ekibi (biz) |
| Business Framework | Ortak çekirdek **aynı**, detaylar farklı | Framework ekibi (generic core) |
| Proje Katmanı | Kurumdan kuruma **farklı** | Proje ekibi (extension) |

---

## 3. Yaklaşım: Base Entity + Inheritance

```csharp
// ═══ Business Framework'te (NuGet veya Shared) ═══

public class CustomerBase : AuditableEntity, ITenantEntity
{
    public string Name { get; set; }
    public string TaxNumber { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public AddressVO Address { get; set; }        // Value Object
    public CustomerType Type { get; set; }         // Individual, Corporate
    public CustomerStatus Status { get; set; }     // Active, Inactive, Blocked
}

// ═══ Proje tarafında ═══

public class Customer : CustomerBase
{
    public decimal CreditLimit { get; set; }       // kuruma özel
    public string BayiKodu { get; set; }           // sektöre özel
    public RiskLevel RiskLevel { get; set; }       // kuruma özel enum
}
```

**Avantajlar:**
- Framework CRUD handler'ları `CustomerBase` ile çalışır → proje tarafında override gerekmez
- Proje `Customer : CustomerBase` ile ekstra alanlar ekler → EF Core migration ile DB'ye yansır
- Tip-güvenli, sorgulanabilir, raporlanabilir

### Kalıtım Stratejisi: TPT (Table-Per-Type)

Entity Framework Core tarafında bu kalıtım **TPT (Table-Per-Type)** stratejisi ile veritabanına yansıtılacaktır. 

- **Nasıl Çalışır:** `CustomerBase` nesnesi `App_CustomerBase` adında bir tabloya, projeye özel `Customer` nesnesi ise sadece projeye özel alanları içeren `Proj_Customer` adında (ve `CustomerBase`'in PK'sine ForeignKey ile bağlı) ayrı bir tabloya dönüştürülür. Sorgularda EF Core bu iki tabloyu `JOIN` ile birleştirir.
- **Neden TPH (Table-Per-Hierarchy) Değil?** TPH her şeyi tek bir `Customers` tablosuna koyar. Çok sayıda proje spesifik kolon eklendiğinde bu tablo NULL değerlerle dolar ve yönetimi zorlaşır. Ayrıca framework tarafına eklenecek yeni bir kolon, projeye özel kolonların da bulunduğu bu devasa tabloyu değiştirmek zorunda kalır.
- **Neden TPC (Table-Per-Concrete-Type) Değil?** TPC, `CustomerBase` gibi temel sınıflar için tablo oluşturmaz, sadece `Customer` tablosu oluşturur. Bu okuma (read) performansını artırsa da, `CustomerBase` üzerinden yapılacak polimorfik sorgularda (Örn: "Tüm müşterileri getir") tüm alt tabloları `UNION ALL` ile birleştirmeye çalışacağı için ciddi performans kaybına yol açar. Ayrıca framework'ün merkezi olarak schema yönetmesini zorlaştırır.
- **TPT'nin Avantajı:** Framework kendi tablolarından (`App_...`), projeler kendi tablolarından (`Proj_...`) sorumludur. Şema ayrımı (Separation of Concerns) kusursuz sağlanır, migration çatışmaları önlenir. Oluşabilecek JOIN maliyetleri ise CQRS yapısındaki Read (Query) modellerinin Dapper veya Materialized View'lar ile desteklenmesiyle kolayca aşılır.

---

## 4. Generic CRUD Altyapısı

Framework her Base entity için temel CRUD operasyonlarını sağlar:

```csharp
// Framework'te — her modül için tekrar yazılmaz
public class CreateEntityCommand<TEntity> : IRequest<Result<Guid>> 
    where TEntity : BaseEntity { }

public class GetEntityByIdQuery<TEntity, TDto> : IRequest<Result<TDto>> 
    where TEntity : BaseEntity { }

public class GetEntitiesPagedQuery<TEntity, TDto> : IRequest<Result<PagedResult<TDto>>> 
    where TEntity : BaseEntity { }

public class UpdateEntityCommand<TEntity> : IRequest<Result> 
    where TEntity : BaseEntity { }

public class DeleteEntityCommand<TEntity> : IRequest<Result> 
    where TEntity : BaseEntity { }
```

Proje tarafı özel iş kuralı gerektiren durumlarda **kendi handler'ını yazar** ve generic olanı override eder.

---

## 5. Extension Mekanizması

```
Proje tarafı framework davranışını 3 şekilde genişletir:

1. Entity Inheritance  → CustomerBase'e alan ekle
2. Handler Override    → Generic handler yerine özel handler yaz
3. Domain Event Hook   → Framework event'ine kendi listener'ını bağla
```

**Örnek — Fırsat kapanma kuralı:**
```
Framework:  OpportunityBase.Stage = Won (generic, sadece status değiştirir)
Proje:      WhenOpportunityWon → komisyon hesapla, bölge müdürüne bildir (kuruma özel)
```

Framework event'i yayınlar (`OpportunityStageChangedEvent`), proje tarafı bu event'e istediği iş kuralını bağlar.

---

## 6. Modül Bazlı Generic Core Tanımları

### CRM

| Base Entity | Temel Alanlar | İlişkiler |
|-------------|---------------|-----------|
| `CustomerBase` | Name, TaxNumber, Email, Phone, Address, Type, Status | → ContactBase (1:N) |
| `ContactBase` | FirstName, LastName, Email, Phone, Title, IsPrimary | → CustomerBase (N:1) |
| `OpportunityBase` | Title, Amount, Stage, Probability, ExpectedCloseDate | → CustomerBase, ContactBase |
| `ActivityBase` | Type (Call/Meeting/Note), Subject, Date, Notes | → CustomerBase, OpportunityBase |

**Temel use case'ler:** Müşteri CRUD, fırsat aşama geçişi, aktivite kaydı, müşteri arama/filtreleme

---

### Sales

| Base Entity | Temel Alanlar | İlişkiler |
|-------------|---------------|-----------|
| `SalesOrderBase` | OrderNumber, OrderDate, Status, TotalAmount, Currency | → CustomerBase |
| `OrderItemBase` | ProductName, Quantity, UnitPrice, Discount, LineTotal | → SalesOrderBase |
| `PriceListBase` | Name, Currency, ValidFrom, ValidTo, IsDefault | — |
| `PriceListItemBase` | UnitPrice, MinQuantity | → PriceListBase, ProductBase |

**Temel use case'ler:** Sipariş oluşturma, fiyat listesi uygulama, sipariş durum geçişi, iskonto hesaplama

---

### HR

| Base Entity | Temel Alanlar | İlişkiler |
|-------------|---------------|-----------|
| `EmployeeBase` | FirstName, LastName, Email, HireDate, Department, Position, Status | → Organization |
| `LeaveRequestBase` | LeaveType, StartDate, EndDate, Days, Status, Reason | → EmployeeBase |
| `AttendanceBase` | Date, CheckIn, CheckOut, WorkHours, OvertimeHours | → EmployeeBase |

**Temel use case'ler:** Personel CRUD, izin talebi + onay akışı, puantaj kaydı

---

### Finance

| Base Entity | Temel Alanlar | İlişkiler |
|-------------|---------------|-----------|
| `AccountBase` | Name, Code, Type (Receivable/Payable), Balance, Currency | → CustomerBase (opsiyonel) |
| `InvoiceBase` | InvoiceNumber, Date, DueDate, Type (Sales/Purchase), Status, Total | → CustomerBase, AccountBase |
| `InvoiceItemBase` | Description, Quantity, UnitPrice, TaxRate, LineTotal | → InvoiceBase |
| `PaymentBase` | Amount, Date, Method, Reference, Status | → InvoiceBase, AccountBase |

**Temel use case'ler:** Fatura oluşturma, ödeme kaydı, vade takibi, cari hesap bakiyesi

---

### Inventory

| Base Entity | Temel Alanlar | İlişkiler |
|-------------|---------------|-----------|
| `ProductBase` | Name, SKU, Barcode, Category, Unit, Status | — |
| `WarehouseBase` | Name, Code, Address, IsDefault | — |
| `StockMovementBase` | Type (In/Out/Transfer), Quantity, Date, Reference | → ProductBase, WarehouseBase |

**Temel use case'ler:** Ürün CRUD, stok giriş/çıkış, depo transfer, stok bakiyesi sorgulama

---

### Procurement

| Base Entity | Temel Alanlar | İlişkiler |
|-------------|---------------|-----------|
| `SupplierBase` | Name, TaxNumber, Email, Phone, Address, Status | — |
| `PurchaseRequestBase` | RequestNumber, Date, Status, RequestedBy, Reason | — |
| `PurchaseOrderBase` | OrderNumber, Date, Status, TotalAmount | → SupplierBase |

**Temel use case'ler:** Satın alma talebi + onay, sipariş oluşturma, tedarikçi yönetimi

---

## 7. Veri Yapısı Seviye Seçimi

| Seviye | Framework sağlar | Proje yapar |
|--------|-----------------|-------------|
| **Minimal** | Entity isimleri + PK/FK | Tüm alanları kendisi tanımlar |
| **Orta** ✅ | Base entity + temel alanlar + ilişkiler + enum'lar | Inherit eder, ekstra alan ekler |
| **Tam** | Entity + alanlar + iş kuralları + UI bileşenleri | Sadece konfigüre eder |

> [!IMPORTANT]
> **Önerilen seviye: Orta.** Tam seviye framework'ü çok rigid yapar (Odoo/SAP sorunu). Minimal seviye ise framework'ün değerini düşürür. Orta seviye: "çalışan bir CRM verir, proje genişletir."

---

## 8. Referans Karşılaştırma

| Platform | Yaklaşım | Business Framework? |
|----------|----------|---------------------|
| **ABP Framework** | Teknik altyapı + opsiyonel iş modülleri (pro lisanslı) | ✅ Ama pro modüller ücretli |
| **Odoo** | Tam business framework (Python) | ✅ Ama çok rigid |
| **ERPNext** | Tam business framework (Python/JS) | ✅ Ama kendi ekosistemi |
| **EntApp.Framework** | Teknik + generic business core (açık, genişletilebilir) | ✅ Hedef |

Fark: EntApp.Framework **orta seviyede** kalarak hem hazır iş modülü sunar hem de proje tarafına tam esneklik bırakır.

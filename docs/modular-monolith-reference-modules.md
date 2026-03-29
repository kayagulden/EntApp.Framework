# Kurumsal Modüler Monolit — Referans Modül Listesi

> **Tarih:** 2026-03-28
> **Amaç:** Kurumsal uygulamalar için genel geçer modül şablonu
> **Not:** Her firma ihtiyacına göre modülleri dahil eder veya çıkarır

---

## Çekirdek Modüller (Core — Hemen Hemen Her Projede Gerekli)

### 1. Identity & Access Management (IAM)
- Kullanıcı yönetimi (CRUD, profil, avatar)
- Rol ve yetki yönetimi (RBAC)
- Organizasyon yapısı (departman, şube, holding hiyerarşisi)
- LDAP/Active Directory entegrasyonu
- SSO (SAML, OAuth2, OpenID Connect)
- Multi-Factor Authentication (MFA)
- Oturum yönetimi, token lifecycle
- Şifre politikaları, şifre sıfırlama
- Login audit log

### 2. Notification
- E-posta gönderimi (SMTP, Exchange, SendGrid)
- SMS gönderimi
- Push notification (mobil)
- In-app bildirimler (WebSocket/SignalR)
- Bildirim şablonları (template engine)
- Bildirim tercihleri (kullanıcı bazlı opt-in/opt-out)
- Bildirim geçmişi ve okunma takibi

### 3. File & Document Management
- Dosya yükleme/indirme (S3, Azure Blob, local disk)
- Versiyon yönetimi
- Dosya önizleme (PDF, resim, Office)
- Dosya paylaşımı ve erişim kontrolü
- Metadata ve tagging
- Çöp kutusu (soft delete + geri yükleme)

### 4. Audit & Logging
- İşlem kaydı (kim, ne zaman, ne yaptı)
- Veri değişiklik geçmişi (entity history)
- Login/logout kayıtları
- Hassas işlem logları (yetki değişikliği, veri silme vb.)
- Log arama ve filtreleme
- KVKK/GDPR uyumluluk logları

### 5. Configuration & Settings
- Uygulama parametreleri (key-value store)
- Tenant/şube bazlı konfigürasyon
- Feature flags (özellik açma/kapama)
- Bakım modu yönetimi

---

## İş Süreçleri Modülleri (Business)

### 6. Workflow & Approval Engine
- İş akışı tanımlama (onay süreçleri)
- Çok aşamalı onay (sıralı, paralel)
- Yetki bazlı onay zinciri
- Zaman aşımı ve eskalasyon
- İş akışı geçmişi ve takip
- Dinamik form desteği

### 7. Task & Project Management
- Görev oluşturma ve atama
- Proje planlama (Gantt, Kanban)
- Alt görevler ve checklist
- Süre takibi (time tracking)
- Öncelik ve etiket yönetimi
- Görev yorumları ve dosya ekleri

### 8. CRM (Müşteri İlişkileri)
- Müşteri/firma kaydı
- İletişim geçmişi
- Satış fırsatı (opportunity) takibi
- Teklif yönetimi
- Sözleşme yönetimi
- Müşteri segmentasyonu

### 9. HR (İnsan Kaynakları)
- Personel kaydı ve özlük bilgileri
- Organizasyon şeması
- İzin yönetimi (yıllık, mazeret, rapor)
- Mesai ve puantaj
- Performans değerlendirme
- İşe alım süreci (aday takip)
- Eğitim planlaması

### 10. Finance & Accounting
- Cari hesap yönetimi
- Fatura oluşturma ve takip
- Gelir/gider kayıtları
- Bütçe planlama
- Ödeme takibi (vadesi gelen/geçen)
- Banka entegrasyonu
- Muhasebe entegrasyonu (e-defter, e-fatura)

### 11. Inventory & Asset Management
- Stok yönetimi (ürün, malzeme)
- Depo yönetimi (çoklu depo)
- Stok hareketleri (giriş, çıkış, transfer)
- Demirbaş takibi
- Barkod/QR entegrasyonu
- Min/max stok uyarıları

### 12. Procurement (Satın Alma)
- Satın alma talebi
- Teklif toplama ve karşılaştırma
- Sipariş yönetimi
- Tedarikçi yönetimi ve puanlama
- Fatura eşleştirme (3-way matching)

### 13. Sales & Order Management (Satış ve Sipariş)
- Satış siparişi oluşturma (SalesOrder)
- Sipariş kalemleri (fiyat, miktar, iskonto)
- Sipariş durumu takibi (Onay → Hazırlık → Sevk → Teslim)
- Fiyat listesi ve iskonto kuralları
- Sipariş onay akışı (Workflow modülüne bağlanır)
- Sevkiyat planı ve teslimat takibi
- İade/iptal yönetimi
- Satış raporları (Reporting modülüne veri)

### 14. Merchant Management (Üye İş Yeri Yönetimi)
- Merchant kayıt ve profil yönetimi
- Merchant kategorileri ve segmentasyon
- Merchant lokasyonları/şubeleri (harita entegrasyonu)
- Sözleşme ve komisyon yönetimi
- Onboarding süreci (aday → müzakere → entegrasyon → aktivasyon)
- Performans takibi (ciro, işlem hacmi, müşteri skoru)
- Churn riski tespiti ve geri kazanım
- POS/terminal entegrasyonu
- Saha ziyareti planlaması

> [!NOTE]
> Merchant, klasik tedarikçi değildir. Tedarikçi firmadan mal/hizmet sağlar (Procurement). Merchant ise firmanın **ağının parçasıdır** — son müşteriye hizmet verir, platform üzerinden gelir üretir.

### 15. Consumer Management (Son Tüketici Yönetimi)
- Tüketici profili ve hesap yönetimi
- B2C bireysel tüketici + B2B2C kurumsal müşteri çalışanları
- Kurumsal bağlantı (CorporateAffiliation — hangi firmaya bağlı)
- Tüketici segmentasyonu
- Kart/üyelik yönetimi (ön ödemeli kart, dijital kart)
- Tercih ve bildirim ayarları
- Tüketici işlem geçmişi

### 16. Loyalty & Sadakat
- Sadakat programı tanımı (puan, cashback, mil)
- Puan kazanma/harcama kuralları
- Tier yönetimi (bronz, gümüş, gold)
- Kampanya ve promosyon
- Ödül kataloğu
- Merchant ve dijital kanal için geçerli

### 17. Digital Sales (Dijital Satış Kanalı)
- Mobil uygulama + web satışları
- Ürün/hizmet kataloğu (dijital kanal özel)
- Sepet ve sipariş yönetimi
- Ödeme entegrasyonu (online)
- Dijital teslimat (e-kart, kod, kupon)
- Fiziksel teslimat desteği (ön ödemeli kart gönderimi vb.)
- Kampanya entegrasyonu (Loyalty ile)

### 18. Service Delivery (Hizmet Teslimi — B2B2C)
- Hizmet lokasyonları (müşterinin sahaları)
- Operasyonel personel atama ve vardiya
- Hizmet kalitesi ve SLA takibi
- Operasyonel raporlama
- Olay/şikayet kaydı

> [!NOTE]
> Service Delivery şu an aktif olarak kullanılmıyor (kart gönderimi dışında) ancak B2B2C operasyonlarının büyümesi durumunda hazır altyapı sağlar.

---

## İletişim ve İşbirliği Modülleri

### 13. Messaging & Collaboration
- Anlık mesajlaşma (1-1, grup)
- Kanallar/odalar
- Dosya paylaşımı (chat içi)
- Mention ve bildirimler
- Mesaj arama

### 14. Calendar & Scheduling
- Kişisel ve takım takvimi
- Toplantı planlama
- Oda/kaynak rezervasyonu
- Takvim paylaşımı
- Outlook/Google Calendar sync

### 15. Knowledge Base & Wiki
- Bilgi bankası makaleleri
- Kategorilendirme
- Arama ve filtreleme
- Versiyon yönetimi
- Erişim kontrolü (departman bazlı)

---

## Raporlama ve Analiz Modülleri

### 16. Reporting & Analytics
- Standart raporlar (önceden tanımlı)
- Dinamik rapor oluşturucu (drag & drop)
- Dashboard'lar (widget bazlı)
- Excel/PDF/CSV export
- Zamanlanmış rapor gönderimi
- KPI takibi
- Grafik ve görselleştirme

### 17. Data Import/Export
- Toplu veri aktarımı (CSV, Excel, XML)
- Veri doğrulama kuralları
- Import geçmişi ve hata raporu
- Otomatik scheduled import (FTP, API)

---

## Entegrasyon Modülleri

### 18. Integration Hub
- REST API gateway (iç ve dış API'ler)
- Webhook yönetimi (inbound/outbound)
- Message queue entegrasyonu (RabbitMQ, Kafka)
- ERP entegrasyonu (SAP, Logo, Netsis)
- E-devlet entegrasyonları (e-fatura, KPS, SGK)
- Banka entegrasyonları
- Harici sistem connector'ları

### 19. API Management
- API versiyon yönetimi
- Rate limiting ve throttling
- API key / OAuth2 yönetimi
- API dokümantasyonu (Swagger/OpenAPI)
- API kullanım analitik

---

## Altyapı Modülleri (Cross-Cutting)

### 20. Multi-Tenancy
- Tenant yönetimi (firma/şube izolasyonu)
- Tenant bazlı veri izolasyonu
- Tenant bazlı konfigürasyon ve tema
- Tenant arası veri paylaşımı kuralları

### 21. Localization & i18n
- Çoklu dil desteği
- Tarih/saat/para birimi formatları
- Dinamik çeviri yönetimi
- RTL desteği (Arapça vb.)

### 22. Background Jobs
- Uzun süren iş kuyruğu
- Cron-based zamanlanmış görevler
- Job monitoring ve retry
- Job geçmişi ve hata logları

### 23. Cache & Performance
- Distributed cache (Redis)
- Sorgu optimizasyonu
- Response caching
- CDN entegrasyonu (statik dosyalar)

---

## Opsiyonel / Sektöre Özel Modüller

### 24. Field Service
- Saha ekibi yönetimi
- İş emri oluşturma ve atama
- Lokasyon takibi (GPS)
- Mobil check-in/check-out
- Saha raporu

### 26. Help Desk / Ticket
- Destek talebi yönetimi
- SLA takibi
- Önceliklendirme ve eskalasyon
- Müşteri portalı
- Bilgi bankası entegrasyonu

### 27. Survey & Feedback
- Anket oluşturucu
- Müşteri memnuniyet anketi (NPS, CSAT)
- Çalışan anketi
- Sonuç analizi ve raporlama

---

## Modüller Arası Bağımlılık Haritası

```
IAM (Core) ──────► HER MODÜL (zorunlu bağımlılık)
Notification ────► HER MODÜL (opsiyonel bağımlılık)
Audit ───────────► HER MODÜL (cross-cutting)
File Management ─► Workflow, CRM, HR, Task, Help Desk

Workflow ────► Task, Procurement, HR, Finance, Sales
Calendar ────► HR, Task, CRM
Reporting ───► TÜM iş modülleri (veri kaynağı olarak)
Integration ─► Finance, CRM, HR, Inventory (dış sistemler)
Multi-Tenancy ► TÜM modüller (veri izolasyonu)

CRM (kurumsal müşteri) ──► ConsumerManagement (B2B2C çalışanları)
ConsumerManagement ──► MerchantManagement (merchant'ta işlem)
                   ──► Loyalty (puan kazanma/harcama)
                   ──► DigitalSales (mobil/web satış)

CRM (müşteri) ──► Sales (B2B sipariş) ──► Inventory (stok düşümü)
                                      ──► Finance (fatura oluşturma)
                                      ──► Notification (sipariş bildirimi)

MerchantManagement ──► Finance (komisyon, ödeme mutabakatı)
                   ──► Reporting (merchant performans raporları)
                   ──► Workflow (onboarding onay akışı)
                   ──► Integration (POS/terminal entegrasyonu)
                   ──► Calendar (saha ziyaret planı)

ServiceDelivery ──► HR (personel bilgisi)
                ──► CRM (müşteri lokasyonu)
                ──► Calendar (vardiya/ziyaret planı)

Procurement (tedarikçi) ←─ BAĞIMSIZ ─→ MerchantManagement (merchant)
```

---

## Modüler Monolit'te Modül Sınırları

Her modül şu kurallara uymalıdır:

1. **Kendi DB şeması** — modüller arası doğrudan tablo join yok
2. **Tanımlı API kontratı** — modüller arası iletişim sadece interface/event üzerinden
3. **Bağımsız deploy edilebilirlik** — şart değil ama ayrılabilir olmalı
4. **Kendi entity'leri** — shared entity yok, gerekirse event ile sync

```
src/
│
├── Modules/
│   │
│   │── ── Çekirdek (Core) ──────────────────────────
│   │
│   ├── IAM/
│   │   ├── IAM.Domain/              (User, Role, Permission, Organization entities)
│   │   ├── IAM.Application/         (use cases, DTOs, interfaces)
│   │   ├── IAM.Infrastructure/      (EF Core, LDAP connector, token service)
│   │   └── IAM.API/                 (controllers, endpoints)
│   │
│   ├── Notification/
│   │   ├── Notification.Domain/     (NotificationTemplate, NotificationLog)
│   │   ├── Notification.Application/
│   │   ├── Notification.Infrastructure/  (SMTP, SMS, SignalR, Push providers)
│   │   └── Notification.API/
│   │
│   ├── FileManagement/
│   │   ├── FileManagement.Domain/   (FileEntry, FileVersion, FileTag)
│   │   ├── FileManagement.Application/
│   │   ├── FileManagement.Infrastructure/  (S3, Azure Blob, local storage)
│   │   └── FileManagement.API/
│   │
│   ├── Audit/
│   │   ├── Audit.Domain/            (AuditLog, EntityChange, LoginRecord)
│   │   ├── Audit.Application/
│   │   ├── Audit.Infrastructure/
│   │   └── Audit.API/
│   │
│   ├── Configuration/
│   │   ├── Configuration.Domain/    (AppSetting, FeatureFlag)
│   │   ├── Configuration.Application/
│   │   ├── Configuration.Infrastructure/
│   │   └── Configuration.API/
│   │
│   │── ── İş Süreçleri (Business) ──────────────────
│   │
│   ├── Workflow/
│   │   ├── Workflow.Domain/         (WorkflowDefinition, WorkflowInstance, ApprovalStep)
│   │   ├── Workflow.Application/
│   │   ├── Workflow.Infrastructure/
│   │   └── Workflow.API/
│   │
│   ├── TaskManagement/
│   │   ├── TaskManagement.Domain/   (Project, TaskItem, Comment, TimeEntry)
│   │   ├── TaskManagement.Application/
│   │   ├── TaskManagement.Infrastructure/
│   │   └── TaskManagement.API/
│   │
│   ├── CRM/
│   │   ├── CRM.Domain/             (Customer, Contact, Opportunity, Contract)
│   │   ├── CRM.Application/
│   │   ├── CRM.Infrastructure/
│   │   └── CRM.API/
│   │
│   ├── HR/
│   │   ├── HR.Domain/              (Employee, Leave, Attendance, Performance)
│   │   ├── HR.Application/
│   │   ├── HR.Infrastructure/
│   │   └── HR.API/
│   │
│   ├── Finance/
│   │   ├── Finance.Domain/         (Account, Invoice, Payment, Budget)
│   │   ├── Finance.Application/
│   │   ├── Finance.Infrastructure/  (banka entegrasyonu, e-fatura)
│   │   └── Finance.API/
│   │
│   ├── Inventory/
│   │   ├── Inventory.Domain/       (Product, Warehouse, StockMovement, Asset)
│   │   ├── Inventory.Application/
│   │   ├── Inventory.Infrastructure/
│   │   └── Inventory.API/
│   │
│   ├── Procurement/
│   │   ├── Procurement.Domain/     (PurchaseRequest, Supplier, PurchaseOrder)
│   │   ├── Procurement.Application/
│   │   ├── Procurement.Infrastructure/
│   │   └── Procurement.API/
│   │
│   │── ── İletişim & İşbirliği ─────────────────────
│   │
│   ├── Messaging/
│   │   ├── Messaging.Domain/       (Conversation, Message, Channel)
│   │   ├── Messaging.Application/
│   │   ├── Messaging.Infrastructure/  (SignalR hub, message store)
│   │   └── Messaging.API/
│   │
│   ├── Calendar/
│   │   ├── Calendar.Domain/        (CalendarEvent, Recurrence, RoomBooking)
│   │   ├── Calendar.Application/
│   │   ├── Calendar.Infrastructure/  (Outlook/Google sync)
│   │   └── Calendar.API/
│   │
│   ├── KnowledgeBase/
│   │   ├── KnowledgeBase.Domain/   (Article, Category, ArticleVersion)
│   │   ├── KnowledgeBase.Application/
│   │   ├── KnowledgeBase.Infrastructure/
│   │   └── KnowledgeBase.API/
│   │
│   │── ── Raporlama & Analiz ───────────────────────
│   │
│   ├── Reporting/
│   │   ├── Reporting.Domain/       (ReportDefinition, Dashboard, Widget, KPI)
│   │   ├── Reporting.Application/
│   │   ├── Reporting.Infrastructure/  (PDF, Excel, chart engine)
│   │   └── Reporting.API/
│   │
│   ├── DataExchange/
│   │   ├── DataExchange.Domain/    (ImportJob, ExportJob, MappingRule)
│   │   ├── DataExchange.Application/
│   │   ├── DataExchange.Infrastructure/  (CSV, Excel, XML parser'lar)
│   │   └── DataExchange.API/
│   │
│   │── ── Entegrasyon ──────────────────────────────
│   │
│   ├── IntegrationHub/
│   │   ├── IntegrationHub.Domain/  (Connector, WebhookSubscription, SyncLog)
│   │   ├── IntegrationHub.Application/
│   │   ├── IntegrationHub.Infrastructure/  (ERP, e-devlet, banka connector'ları)
│   │   └── IntegrationHub.API/
│   │
│   ├── ApiManagement/
│   │   ├── ApiManagement.Domain/   (ApiKey, RateLimit, UsageLog)
│   │   ├── ApiManagement.Application/
│   │   ├── ApiManagement.Infrastructure/
│   │   └── ApiManagement.API/
│   │
│   │── ── Altyapı (Cross-Cutting) ──────────────────
│   │
│   ├── MultiTenancy/
│   │   ├── MultiTenancy.Domain/    (Tenant, TenantSettings)
│   │   ├── MultiTenancy.Application/
│   │   ├── MultiTenancy.Infrastructure/  (tenant resolver, data filter)
│   │   └── MultiTenancy.API/
│   │
│   ├── Localization/
│   │   ├── Localization.Domain/    (Language, TranslationEntry)
│   │   ├── Localization.Application/
│   │   ├── Localization.Infrastructure/
│   │   └── Localization.API/
│   │
│   ├── BackgroundJobs/
│   │   ├── BackgroundJobs.Domain/  (JobDefinition, JobExecution, JobLog)
│   │   ├── BackgroundJobs.Application/
│   │   ├── BackgroundJobs.Infrastructure/  (Hangfire/Quartz adapter)
│   │   └── BackgroundJobs.API/
│   │
│   │── ── Satış & Sipariş ──────────────────────
│   │
│   ├── Sales/
│   │   ├── Sales.Domain/           (SalesOrder, OrderItem, PriceList, Discount)
│   │   ├── Sales.Application/
│   │   ├── Sales.Infrastructure/
│   │   └── Sales.API/
│   │
│   ├── MerchantManagement/
│   │   ├── MerchantManagement.Domain/  (Merchant, MerchantLocation, MerchantContract,
│   │   │                               MerchantOnboarding, MerchantPerformance)
│   │   ├── MerchantManagement.Application/
│   │   ├── MerchantManagement.Infrastructure/  (POS/terminal, harita API)
│   │   └── MerchantManagement.API/
│   │
│   ├── ConsumerManagement/
│   │   ├── ConsumerManagement.Domain/  (Consumer, Membership, CorporateAffiliation)
│   │   ├── ConsumerManagement.Application/
│   │   ├── ConsumerManagement.Infrastructure/
│   │   └── ConsumerManagement.API/
│   │
│   ├── Loyalty/
│   │   ├── Loyalty.Domain/             (LoyaltyProgram, PointTransaction, Tier, Campaign)
│   │   ├── Loyalty.Application/
│   │   ├── Loyalty.Infrastructure/
│   │   └── Loyalty.API/
│   │
│   ├── DigitalSales/
│   │   ├── DigitalSales.Domain/        (DigitalOrder, DigitalProduct, OnlinePayment)
│   │   ├── DigitalSales.Application/
│   │   ├── DigitalSales.Infrastructure/  (ödeme gateway, dijital teslimat)
│   │   └── DigitalSales.API/
│   │
│   ├── ServiceDelivery/              (opsiyonel — B2B2C operasyon)
│   │   ├── ServiceDelivery.Domain/     (ServicePoint, StaffAssignment, ServiceMetric)
│   │   ├── ServiceDelivery.Application/
│   │   ├── ServiceDelivery.Infrastructure/
│   │   └── ServiceDelivery.API/
│   │
│   │── ── Opsiyonel / Sektöre Özel ─────────────────
│   │
│   ├── FieldService/                (opsiyonel)
│   │   ├── FieldService.Domain/    (WorkOrder, FieldAgent, Location)
│   │   ├── FieldService.Application/
│   │   ├── FieldService.Infrastructure/
│   │   └── FieldService.API/
│   │
│   ├── HelpDesk/                    (opsiyonel)
│   │   ├── HelpDesk.Domain/        (Ticket, SLA, EscalationRule)
│   │   ├── HelpDesk.Application/
│   │   ├── HelpDesk.Infrastructure/
│   │   └── HelpDesk.API/
│   │
│   └── Survey/                      (opsiyonel)
│       ├── Survey.Domain/          (SurveyDefinition, Question, Response)
│       ├── Survey.Application/
│       ├── Survey.Infrastructure/
│       └── Survey.API/
│
├── Shared/
│   ├── Shared.Kernel/
│   │   ├── BaseEntity.cs            (Id, CreatedAt, UpdatedAt, IsDeleted)
│   │   ├── AuditableEntity.cs       (CreatedBy, ModifiedBy)
│   │   ├── ITenantEntity.cs         (TenantId)
│   │   ├── ValueObjects/            (Money, DateRange, Address, Email)
│   │   ├── Enums/                   (Status, Priority, vb.)
│   │   └── Exceptions/             (DomainException, NotFoundException)
│   │
│   ├── Shared.Contracts/
│   │   ├── Events/                  (modüller arası integration event'ler)
│   │   │   ├── UserCreatedEvent.cs
│   │   │   ├── OrderCompletedEvent.cs
│   │   │   ├── InvoiceApprovedEvent.cs
│   │   │   └── ...
│   │   ├── DTOs/                    (modüller arası paylaşılan DTO'lar)
│   │   │   ├── UserInfoDto.cs       (IAM'den diğer modüllere)
│   │   │   ├── TenantInfoDto.cs
│   │   │   └── ...
│   │   └── Interfaces/             (modüller arası servis kontratları)
│   │       ├── ICurrentUser.cs
│   │       ├── ICurrentTenant.cs
│   │       └── IEventBus.cs
│   │
│   └── Shared.Infrastructure/
│       ├── Persistence/
│       │   ├── BaseDbContext.cs      (audit, soft delete, tenant filter)
│       │   └── UnitOfWork.cs
│       ├── Auth/
│       │   ├── JwtTokenService.cs
│       │   └── PermissionAuthorizationHandler.cs
│       ├── Caching/                  (#23 Cache & Performance — cross-cutting)
│       │   ├── ICacheService.cs      (abstraction)
│       │   ├── RedisCacheService.cs  (distributed cache)
│       │   ├── InMemoryCacheService.cs (local fallback)
│       │   ├── CacheKeyBuilder.cs    (modül bazlı key üretimi)
│       │   └── CacheInvalidationService.cs
│       ├── Performance/             (#23 Cache & Performance — cross-cutting)
│       │   ├── ResponseCachingMiddleware.cs
│       │   ├── QueryPerformanceInterceptor.cs (yavaş sorgu logla)
│       │   ├── CompressionMiddleware.cs
│       │   └── RateLimitingMiddleware.cs
│       ├── EventBus/
│       │   └── InMemoryEventBus.cs   (modüler monolit: in-process)
│       ├── Middleware/
│       │   ├── TenantResolutionMiddleware.cs
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   ├── AuditMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs
│
├── Host/
│   └── WebAPI/
│       ├── Program.cs               (composition root)
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── appsettings.Production.json
│       ├── ModuleRegistration.cs     (tüm modülleri DI'ya register eder)
│       └── Dockerfile
│
├── Tests/
│   ├── Modules/
│   │   ├── IAM.Tests/
│   │   ├── CRM.Tests/
│   │   ├── Finance.Tests/
│   │   ├── HR.Tests/
│   │   └── ...                      (her modülün kendi test projesi)
│   ├── Integration/
│   │   └── IntegrationTests/        (modüller arası entegrasyon testleri)
│   └── Shared/
│       └── TestHelpers/             (mock factory, test fixtures)
│
├── Database/
│   ├── Migrations/
│   │   ├── IAM/                     (her modülün kendi migration'ları)
│   │   ├── CRM/
│   │   ├── Finance/
│   │   └── ...
│   ├── Seeds/                       (başlangıç verileri)
│   └── Scripts/                     (migration helper script'leri)
│
├── Docs/
│   ├── Architecture.md
│   ├── ModuleGuide.md
│   ├── API/                         (OpenAPI/Swagger export'ları)
│   └── Database/                    (ER diyagramları)
│
├── docker-compose.yml               (PostgreSQL, Redis, RabbitMQ)
├── .editorconfig
├── Directory.Build.props            (global paket versiyonları)
└── Solution.sln
```


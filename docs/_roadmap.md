# EntApp.Framework — Geliştirme Yol Haritası (Roadmap)

> **Tarih:** 2026-03-28  
> **Kaynak dokümanlar:**  
> - [Mimari Spesifikasyon](file:///c:/Users/kaya/projects/EntApp.Framework/enterprise-framework-evaluation.md)  
> - [Business Framework](file:///c:/Users/kaya/projects/EntApp.Framework/business-framework-layer.md)  
> - [AI Entegrasyon](file:///c:/Users/kaya/projects/EntApp.Framework/ai-integration-strategy.md)  
> - [Dinamik UI](file:///c:/Users/kaya/projects/EntApp.Framework/dynamic-ui-generation.md)  
> **İlke:** Küçük fazlar, her faz kendi başına çalışıp test edilebilir

---

## Faz 1 — Solution Yapısı & Docker Compose

**Hedef:** Proje iskeletini oluştur, geliştirme ortamını ayağa kaldır.

- [ ] .NET 9 solution oluştur (`EntApp.sln`)
- [ ] `Directory.Build.props` — global paket versiyonları, C# 13, nullable enable
- [ ] `.editorconfig` — kod formatlama kuralları
- [ ] Klasör yapısı: `src/`, `tests/`, `database/`, `docs/`, `frontend/`
- [ ] `docker-compose.yml` — PostgreSQL 16, Redis 7, RabbitMQ, Keycloak, Seq, Jaeger
- [ ] `docker-compose.override.yml` — geliştirme ortamı ayarları (port, volume)
- [ ] Docker Compose ile tüm servisleri ayağa kaldır ve test et
- [ ] `.gitignore`, `README.md`

**Çıktı:** `docker-compose up` ile tüm altyapı servislerinin çalışır hali.

---

## Faz 2 — Shared.Kernel

**Hedef:** Tüm modüllerin paylaştığı temel yapı taşlarını oluştur.

- [ ] `Shared.Kernel` class library projesi
- [ ] `BaseEntity.cs` — Id (Guid), CreatedAt, UpdatedAt, IsDeleted (soft delete)
- [ ] `AuditableEntity.cs` — CreatedBy, ModifiedBy (BaseEntity'den türer)
- [ ] `AggregateRoot.cs` — `_domainEvents` listesi, `AddDomainEvent()`, `ClearDomainEvents()`
- [ ] `ITenantEntity.cs` — TenantId interface
- [ ] `IDomainEvent.cs` — marker interface (MediatR `INotification`)
- [ ] `Result<T>` — IsSuccess, IsFailure, Value, Error, Errors
- [ ] `Error.cs` — Code, Message, Type (Validation, NotFound, Conflict, Unauthorized)
- [ ] Value Objects: `Money`, `DateRange`, `Address`, `Email`, `PhoneNumber`
- [ ] Enums: `Status`, `Priority`
- [ ] Exceptions: `DomainException`, `NotFoundException`, `ConflictException`
- [ ] Unit testler (xUnit) — Result, ValueObjects, BaseEntity

**Çıktı:** Tüm modüllerin referans edebileceği sıfır bağımlılıklı kernel paketi.

---

## Faz 3 — Shared.Contracts & Shared.Infrastructure

**Hedef:** Modüller arası kontratları ve ortak altyapıyı oluştur.

### 3a — Shared.Contracts
- [ ] `Shared.Contracts` class library projesi
- [ ] `IIntegrationEvent.cs` — modüller arası event kontratı
- [ ] `ICurrentUser.cs` — UserId, UserName, Roles, Permissions
- [ ] `ICurrentTenant.cs` — TenantId, TenantName
- [ ] `IEventBus.cs` — `PublishAsync<T>(T @event)` abstraction
- [ ] `IUnitOfWork.cs` — `SaveChangesAsync()`, `BeginTransactionAsync()`
- [ ] Ortak DTO'lar: `UserInfoDto`, `TenantInfoDto`, `PagedResult<T>`, `LookupDto`

### 3b — Shared.Infrastructure: Persistence
- [ ] `Shared.Infrastructure` class library projesi
- [ ] `BaseDbContext.cs` — audit fields otomatik set, soft delete filter, tenant filter, domain event dispatch
- [ ] `OutboxMessage.cs` entity + `OutboxProcessor.cs` — integration event'leri Outbox'tan publish
- [ ] EF Core interceptors: `AuditableEntityInterceptor`, `SoftDeleteInterceptor`, `DomainEventDispatchInterceptor`

### 3c — Shared.Infrastructure: Pipeline Behaviors
- [ ] `ValidationBehavior.cs` — FluentValidation entegrasyonu
- [ ] `LoggingBehavior.cs` — request/response Serilog ile loglama
- [ ] `PerformanceBehavior.cs` — >500ms süren sorgulara warning log
- [ ] `TransactionBehavior.cs` — Command'lar için begin/commit/rollback
- [ ] `CachingBehavior.cs` — `ICacheableQuery<T>` marker ile Redis cache

### 3d — Shared.Infrastructure: EventBus & Cache
- [ ] `RabbitMQEventBus.cs` — MassTransit + RabbitMQ transport (Başlangıç)
- [ ] `InMemoryEventBus.cs` — geliştirme/test ortamı için
- [ ] `ICacheService.cs`, `RedisCacheService.cs`, `CacheKeyBuilder.cs`

### 3e — Shared.Infrastructure: Middleware
- [ ] `ExceptionHandlingMiddleware.cs` — global hata yakalama, Result dönüş
- [ ] `RequestLoggingMiddleware.cs` — HTTP request loglama
- [ ] `TenantResolutionMiddleware.cs` — header/subdomain/claim'den tenant belirleme
- [ ] `AuditMiddleware.cs` — hassas işlem loglama

### 3f — Shared.Infrastructure: Auth & Health
- [ ] `KeycloakTokenService.cs` — Keycloak JWT doğrulama
- [ ] `PermissionAuthorizationHandler.cs` — policy-based RBAC
- [ ] `ModuleHealthCheck.cs` — modül bazlı health check

### 3g — Shared.Infrastructure: RealTime
- [ ] `EntAppHub.cs` — merkezi SignalR hub
- [ ] `EntityChangeNotifier.cs` — entity değişikliğini push et
- [ ] `UserConnectionTracker.cs` — hangi kullanıcı neyi izliyor

**Çıktı:** Tüm cross-cutting concern'ler çalışır durumda, unit testler yeşil.

---

## Faz 4 — Host/WebAPI & Composition Root

**Hedef:** Uygulamayı ayağa kaldıracak ana proje.

- [ ] `Host/WebAPI` projesi — ASP.NET Core Web API
- [ ] `Program.cs` — composition root (DI, middleware, auth, Serilog, Swagger/Scalar)
- [ ] `ModuleRegistration.cs` — modülleri tek yerden register etme mekanizması
- [ ] `appsettings.json` — PostgreSQL, Redis, RabbitMQ, Keycloak, Seq bağlantı bilgileri
- [ ] `appsettings.Development.json` — geliştirme ortamı override
- [ ] Keycloak realm konfigürasyonu (realm, client, roles)
- [ ] Swagger/Scalar endpoint çalışır durumda
- [ ] Health check endpoint (`/health`)
- [ ] Serilog → Seq entegrasyonu çalışır
- [ ] OpenTelemetry → Jaeger tracing çalışır
- [ ] `Dockerfile` — multi-stage build
- [ ] API versioning (`Asp.Versioning`) konfigürasyonu
- [ ] **Walking Skeleton:** Temel altyapının çalıştığını kanıtlamak için basit bir uçtan uca (End-to-End) API modülünün devreye alınması.

**Çıktı:** Boş bir API ayağa kalkar, `/health`, `/swagger` endpointleri çalışır, loglar Seq'e gider.

---

## Faz 5 — Frontend Scaffold

**Hedef:** Frontend iskeletini oluştur.

- [ ] Next.js 15 projesi (App Router, TypeScript)
- [ ] Tailwind CSS 4 + shadcn/ui konfigürasyonu (`components.json`)
- [ ] Temel shadcn/ui bileşenlerini ekle (button, input, select, dialog, sheet, table, toast, combobox)
- [ ] `globals.css` — tema renkleri (dark/light), font (Inter)
- [ ] `next-themes` — dark/light mode toggle
- [ ] `providers.tsx` — QueryClient, ThemeProvider, AuthProvider
- [ ] Layout: sidebar (collapsible), header (user menu, notification bell, theme toggle), breadcrumb
- [ ] Keycloak login entegrasyonu (OAuth2 PKCE redirect)
- [ ] Orval Kurulumu — Swagger/OpenAPI spec'inden TypeScript tiplerinin ve Axios hook'larının otomatik üretilmesi (Type-safety)
- [ ] Axios/fetch instance — base URL, JWT interceptor, refresh token
- [ ] Zustand store: `useAuthStore`, `useUiStore`
- [ ] Boş dashboard sayfası (placeholder)

**Çıktı:** Login → sidebar'lı boş dashboard, dark/light mode çalışır.

---

## Faz 6 — IAM Modülü

**Hedef:** Kimlik ve erişim yönetimi.

### 6a — IAM Domain & Application
- [ ] `IAM.Domain` — User, Role, Permission, Organization, Department entity'leri
- [ ] `IAM.Application` — Commands: CreateUser, UpdateUser, AssignRole, CreateOrganization
- [ ] `IAM.Application` — Queries: GetUserById, GetUsersPaged, GetRoles, GetOrganizationTree
- [ ] FluentValidation validator'ları
- [ ] Domain Events: `UserCreatedEvent`, `RoleAssignedEvent`, `UserDeactivatedEvent`
- [ ] Integration Events: `UserCreatedIntegrationEvent`, `UserDeactivatedIntegrationEvent`

### 6b — IAM Infrastructure
- [ ] `IamDbContext.cs` — EF Core, PostgreSQL, kendi şeması (`iam.`)
- [ ] Keycloak connector — kullanıcı sync (Keycloak ↔ IAM DB)
- [ ] EF Core migrations
- [ ] Seed data: varsayılan roller (Admin, Manager, User, ReadOnly)

### 6c — IAM API & Frontend
- [ ] Controllers: UserController, RoleController, OrganizationController
- [ ] Frontend: Kullanıcı listesi, kullanıcı formu, rol atama, organizasyon ağacı

**Çıktı:** Kullanıcı CRUD, rol/yetki yönetimi, Keycloak SSO çalışır.

---

## Faz 7 — Diğer Core Modüller

### 7a — Audit Modülü
- [ ] AuditLog, EntityChange, LoginRecord entity'leri
- [ ] EF Interceptor ile otomatik audit log yazma
- [ ] Audit log listeleme ve filtreleme API + UI
- [ ] Temporal Data altyapısı: `[Temporal]` attribute, `_history` tablosu otomatik oluşturma, history/diff API

### 7b — Configuration Modülü
- [ ] AppSetting (key-value), FeatureFlag entity'leri
- [ ] Tenant/global bazlı konfigürasyon desteği
- [ ] Feature flag açma/kapama API
- [ ] Bakım modu mekanizması
- [ ] Frontend: ayarlar sayfası, feature flag yönetimi

### 7c — Notification Modülü
- [ ] NotificationTemplate, NotificationLog entity'leri
- [ ] Provider'lar: SMTP e-posta, SMS (opsiyonel), SignalR in-app, push (opsiyonel)
- [ ] Template engine (Scriban veya Razor) — dinamik şablon
- [ ] Kullanıcı bildirim tercihleri (opt-in/opt-out)
- [ ] Bildirim geçmişi ve okunma takibi
- [ ] SignalR real-time hub entegrasyonu — in-app toast bildirim
- [ ] Frontend: bildirim çanı, bildirim listesi, okundu işaretleme

### 7d — FileManagement Modülü
- [ ] FileEntry, FileVersion, FileTag entity'leri
- [ ] Storage provider abstraction: local disk, S3, Azure Blob
- [ ] Dosya yükleme/indirme API
- [ ] Dosya önizleme (PDF, resim)
- [ ] Versiyon yönetimi, metadata, soft delete + geri yükleme

### 7e — MultiTenancy Modülü
- [ ] Tenant, TenantSettings entity'leri
- [ ] Tenant CRUD API
- [ ] `TenantResolutionMiddleware` — header/subdomain/claim'den tenant çözme
- [ ] EF Core global query filter (`TenantId == currentTenant`)
- [ ] **Tenant Bootstrapper:** `ITenantSeeder` interface, yeni tenant oluşturulduğunda modüllerin seed çalıştırması

### 7f — Localization Modülü
- [ ] Language, TranslationEntry entity'leri
- [ ] Dinamik çeviri yönetimi (DB'den)
- [ ] API: çeviri listesi, çeviri güncelleme
- [ ] Frontend: `next-intl` entegrasyonu, dil seçici

**Çıktı:** Tüm core modüller çalışır, test edilmiş, frontend sayfaları mevcut.

---

## Faz 8 — Dynamic UI Engine

### 8a — Backend Metadata Engine
- [ ] `DynamicEntityAttribute`, `DynamicFieldAttribute`, `DynamicLookupAttribute`, `DynamicDetailAttribute`
- [ ] `MetadataService.cs` — reflection ile entity'den JSON schema üretimi
- [ ] `DynamicCrudEndpointBuilder.cs` — `app.MapDynamicCrudEndpoints()` ile otomatik endpoint registration
- [ ] `DynamicMenuBuilder.cs` — `GET /api/meta/menu` otomatik menu üretimi
- [ ] Generic CRUD handler'lar: `CreateEntityCommand<T>`, `GetEntitiesPagedQuery<T>`, `UpdateEntityCommand<T>`, `DeleteEntityCommand<T>`
- [ ] Lookup endpoint: `GET /api/{entity}/lookup?search=abc`

### 8b — Frontend Render Engine
- [ ] `DynamicPage.tsx` — orchestrator component
- [ ] `DynamicTable.tsx` — TanStack Table + shadcn, metadata'dan otomatik kolon
- [ ] `DynamicForm.tsx` — React Hook Form + Zod, metadata'dan otomatik form
- [ ] `DynamicField.tsx` — field type → shadcn component router (Input, Select, DatePicker, Switch, Combobox...)
- [ ] `DynamicLookup.tsx` — async arama destekli combobox
- [ ] `DynamicDetailTable.tsx` — master-detail alt tablo (inline edit, satır ekle/sil)
- [ ] `DynamicFilters.tsx` — gelişmiş filtreleme paneli
- [ ] `DynamicToolbar.tsx` — butonlar (Yeni Ekle, Dışa Aktar, Filtre)
- [ ] `useDynamicMeta.ts` hook — metadata fetch + cache
- [ ] `useDynamicCrud.ts` hook — CRUD operations
- [ ] `schema-to-zod.ts` — metadata → Zod validation schema üretimi
- [ ] Otomatik sidebar menu (meta/menu endpoint'inden)
- [ ] Override mekanizmaları: config, fieldOverrides, detailOverrides

### 8c — Import/Export Engine
- [ ] `DynamicExportService.cs` — entity → Excel (ClosedXML), CSV (CsvHelper), PDF (QuestPDF)
- [ ] `DynamicImportService.cs` — Excel/CSV parse + validate + bulk insert
- [ ] `ColumnMapper.cs` — Excel kolon → entity field otomatik eşleştirme
- [ ] `ExportTemplateBuilder.cs` — boş import şablonu üretimi
- [ ] `GET /api/{entity}/export?format=xlsx`
- [ ] `GET /api/{entity}/import-template`
- [ ] `POST /api/{entity}/import`
- [ ] Frontend: `DynamicImport.tsx` — 3 adımlı wizard (dosya yükle → kolon eşle → önizle + aktar)
- [ ] Frontend: `DynamicExport.tsx` — format seçimi dialogu

### 8d — Real-time Entegrasyon
- [ ] `DynamicTable` SignalR subscribe — yeni kayıt geldiğinde tablo otomatik yenilenir
- [ ] Entity silme → satır soluklaşıp kaybolma animasyonu
- [ ] Bildirim badge güncelleme (real-time)

### 8e — Test & Demo
- [ ] Lookup entity'ler ile test: Country, City, Currency (DynamicEntity attribute ile)
- [ ] Master-detail test: SalesOrder → OrderItem
- [ ] Import/export testi

**Çıktı:** Attribute ekle → otomatik CRUD ekranı, master-detail, import/export, real-time güncelleme.

---

## Faz 9 — AI Module

### 9a — AI Altyapısı
- [ ] `AI.Domain` — AiModel, PromptTemplate, EmbeddingDocument, AiUsageLog entity'leri
- [ ] `AI.Application` — ILlmService, IEmbeddingService, IRagService, IPromptManager, IDocumentProcessor interfaces
- [ ] Microsoft Semantic Kernel entegrasyonu
- [ ] Provider'lar: `OpenAiProvider`, `AnthropicProvider`, `AzureOpenAiProvider`, `OllamaProvider`
- [ ] `appsettings.json` ile provider seçimi ve fallback

### 9b — Embedding & Vector Search
- [ ] PostgreSQL pgvector extension kurulumu
- [ ] `PgVectorStore.cs` — embedding CRUD + similarity search
- [ ] `EmbeddingDocument` tablosu: tenant_id, module_name, content, embedding, metadata
- [ ] Embedding API: `POST /api/ai/embed`, `POST /api/ai/search`

### 9c — RAG Pipeline
- [ ] `DocumentProcessor` — PDF, Office → düz metin çıkarma
- [ ] Doküman chunking (metin parçalama)
- [ ] Chunk → embedding → pgvector'e kaydet
- [ ] RAG: query embedding → benzer chunk bul → LLM'e context olarak ver → yanıt üret
- [ ] `POST /api/ai/rag` endpoint

### 9d — Prompt Management
- [ ] PromptTemplate CRUD API
- [ ] Versiyonlama (aynı key, farklı version)
- [ ] Şablon engine (Scriban) — `{{variable}}` desteği
- [ ] Frontend: prompt düzenleme ekranı (Admin Panel'e entegre)

### 9e — Maliyet Kontrolü
- [ ] `AiUsageLog` — her LLM çağrısı: modül, token sayısı, maliyet, süre
- [ ] Rate limiting (modül/tenant bazlı)
- [ ] Model routing: basit iş → küçük model, karmaşık → büyük model
- [ ] LLM response caching (Redis)

**Çıktı:** LLM abstraction, embedding/search, RAG, prompt yönetimi çalışır.

---

## Faz 10 — Workflow & Approval Engine

- [ ] Elsa Workflows 3.x NuGet entegrasyonu
- [ ] `Workflow.Domain` — WorkflowDefinition, WorkflowInstance, ApprovalStep entity'leri
- [ ] Basit onay akışı tanımlama API (sıralı, paralel)
- [ ] Yetki bazlı onay zinciri
- [ ] Zaman aşımı ve eskalasyon
- [ ] Workflow geçmişi ve takip
- [ ] Dinamik form desteği (onay adımında form gösterme)
- [ ] Frontend: onay bekleyen görevler listesi, onay/red formu
- [ ] `ApprovalCompletedIntegrationEvent` — diğer modüllere bildirim

**Çıktı:** Basit onay akışları oluşturulup çalıştırılabilir.

---

## Faz 11 — Business Framework: Tier 1 Modüller

**Hedef:** En çok kullanılan iş modüllerinin generic core'unu oluştur.

### 11a — CRM
- [ ] `CustomerBase`, `ContactBase`, `OpportunityBase`, `ActivityBase` entity'leri
- [ ] Temel CRUD + arama/filtreleme
- [ ] Müşteri segmentasyonu (enum tabanlı)
- [ ] Fırsat aşama geçişi (pipeline)
- [ ] Integration Events: `CustomerCreatedIntegrationEvent`, `OpportunityWonIntegrationEvent`
- [ ] Dynamic UI ile ekranlar (override mekanizması hazır)

### 11b — HR
- [ ] `EmployeeBase`, `LeaveRequestBase`, `AttendanceBase` entity'leri
- [ ] Personel CRUD, organizasyon şeması
- [ ] İzin talebi + Workflow modülü ile onay akışı entegrasyonu
- [ ] Puantaj kaydı
- [ ] Integration Events: `EmployeeTerminatedIntegrationEvent`

### 11c — Finance
- [ ] `AccountBase`, `InvoiceBase`, `InvoiceItemBase`, `PaymentBase` entity'leri
- [ ] Cari hesap yönetimi, bakiye hesaplama
- [ ] Fatura oluşturma (satış/satın alma)
- [ ] Ödeme kaydı, vade takibi
- [ ] Integration Events: `InvoiceApprovedIntegrationEvent`, `PaymentReceivedIntegrationEvent`

### 11d — Inventory
- [ ] `ProductBase`, `WarehouseBase`, `StockMovementBase` entity'leri
- [ ] Ürün CRUD, barkod/SKU
- [ ] Stok giriş/çıkış/transfer
- [ ] Depo bazlı stok bakiyesi
- [ ] Min/max stok uyarısı (Notification entegrasyonu)

**Çıktı:** CRM, HR, Finance, Inventory generic core modülleri çalışır, Dynamic UI ile ekranları oluşturulmuş.

---

## Faz 12 — Business Framework: Tier 2 Modüller

### 12a — Sales
- [ ] `SalesOrderBase`, `OrderItemBase`, `PriceListBase` entity'leri
- [ ] Sipariş oluşturma, fiyat listesi uygulama, iskonto
- [ ] Sipariş durumu geçişi + Workflow entegrasyonu
- [ ] Inventory ile stok düşümü (Integration Event)
- [ ] Finance ile fatura oluşturma (Integration Event)
- [ ] Saga: Sipariş → Stok → Fatura (MassTransit saga state machine)

### 12b — Procurement
- [ ] `SupplierBase`, `PurchaseRequestBase`, `PurchaseOrderBase` entity'leri
- [ ] Satın alma talebi + onay akışı
- [ ] Sipariş oluşturma, tedarikçi yönetimi
- [ ] Fatura eşleştirme (3-way matching)

### 12c — Task Management
- [ ] `ProjectBase`, `TaskItemBase`, `CommentBase` entity'leri
- [ ] Görev oluşturma/atama, alt görevler
- [ ] Kanban view (Zustand state)
- [ ] Süre takibi (time entry)

### 12d — Modüller Arası Entegrasyon Testleri
- [ ] Sales → Inventory → Finance saga test
- [ ] HR izin → Workflow onay → Notification test
- [ ] CRM → Sales sipariş akışı test

**Çıktı:** Tüm iş modülleri arası event akışları çalışır, saga testleri geçer.

---

## Faz 13 — Admin Panel

- [ ] Admin Panel modülü (ayrı frontend route: `/admin`)
- [ ] **Tenant Yönetimi:** tenant CRUD, konfigürasyon, deaktif etme
- [ ] **Feature Flags:** modül/tenant bazlı açma/kapama toggle'ları
- [ ] **Prompt Yönetimi:** AI prompt şablonları düzenleme, versiyon karşılaştırma
- [ ] **Background Jobs:** Hangfire dashboard embed
- [ ] **AI İstatistikleri:** token kullanımı, maliyet raporu (Recharts grafikleri)
- [ ] **System Health:** modül bazlı health check durumu, DB/Redis/RabbitMQ bağlantısı
- [ ] **Audit Viewer:** kullanıcı aktivite logları, entity change history tarayıcısı
- [ ] **Cache Yönetimi:** Redis cache temizleme, cache hit/miss oranları

**Çıktı:** Tek yerden framework yönetimi.

---

## Faz 14 — CLI Scaffolding

- [ ] `dotnet new` template paketi oluştur
- [ ] `dotnet new entapp-module --name {ModuleName}` komutu
- [ ] Template: Domain, Application, Infrastructure, API projeleri
- [ ] Template: boş DbContext, örnek entity, örnek Command/Query
- [ ] Template: test projesi
- [ ] Template: frontend route klasörü (opsiyonel flag)
- [ ] Template: migration klasörü
- [ ] Template: `README.md` (modül dokümantasyonu)
- [ ] NuGet paketi olarak yayınlama

**Çıktı:** `dotnet new entapp-module --name Logistics` → tüm boilerplate hazır.

---

## Faz 15 — DevOps & Production Hazırlığı

### 15a — CI/CD Pipeline
- [ ] Azure DevOps pipeline YAML: build → test → Docker image → push
- [ ] Backend + frontend ayrı pipeline stage
- [ ] Database migration otomatik çalıştırma (CD)
- [ ] Environment bazlı deployment (dev → staging → production)

### 15b — Monitoring & Observability
- [ ] Prometheus metrics endpoint
- [ ] Grafana dashboard'lar: API response time, error rate, modül başına trafik
- [ ] Serilog → Seq alerting kuralları
- [ ] Jaeger tracing — modüller arası request takibi

### 15c — Kubernetes
- [ ] Kubernetes manifest'leri: Deployment, Service, Ingress, ConfigMap, Secret
- [ ] Helm chart (opsiyonel)
- [ ] PostgreSQL, Redis, RabbitMQ Kubernetes operator'ları veya managed servis konfigürasyonu
- [ ] Horizontal Pod Autoscaler (HPA)

### 15d — Güvenlik
- [ ] OWASP kontrol listesi gözden geçirme
- [ ] Rate limiting (YARP + middleware)
- [ ] CORS konfigürasyonu
- [ ] HTTPS enforcement
- [ ] API key yönetimi (dış sistemler için)

**Çıktı:** CI/CD otomatik, monitoring hazır, Kubernetes'e deploy edilebilir.

---

## Özet Tablo

| Faz | Başlık | Tahmini Süre |
|-----|--------|-------------|
| 1 | Solution & Docker Compose | 1 gün |
| 2 | Shared.Kernel | 2-3 gün |
| 3 | Shared.Contracts & Infrastructure | 1-2 hafta |
| 4 | Host/WebAPI | 2-3 gün |
| 5 | Frontend Scaffold | 3-4 gün |
| 6 | IAM Modülü | 1-2 hafta |
| 7 | Diğer Core Modüller (6 modül) | 3-4 hafta |
| 8 | Dynamic UI Engine | 2-3 hafta |
| 9 | AI Module | 2-3 hafta |
| 10 | Workflow Engine | 1-2 hafta |
| 11 | Business Framework Tier 1 (4 modül) | 3-4 hafta |
| 12 | Business Framework Tier 2 (3 modül + entegrasyon) | 3-4 hafta |
| 13 | Admin Panel | 1-2 hafta |
| 14 | CLI Scaffolding | 3-4 gün |
| 15 | DevOps & Production | 1-2 hafta |
| | **Toplam** | **~20-28 hafta** |

> [!NOTE]
> Süre tahminleri AI-assisted geliştirme ile hesaplanmıştır. Fazlar sıralıdır ancak bazı fazlar paralel çalışabilir (örn: Faz 5 frontend, Faz 3 backend ile eş zamanlı).

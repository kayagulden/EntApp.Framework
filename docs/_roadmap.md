# EntApp.Framework — Geliştirme Yol Haritası (Roadmap)

> **Tarih:** 2026-03-28  
> **Kaynak dokümanlar:**  
> - [Mimari Spesifikasyon](file:///c:/Users/kaya/projects/EntApp.Framework/docs/enterprise-framework-evaluation.md)  
> - [Business Framework](file:///c:/Users/kaya/projects/EntApp.Framework/docs/business-framework-layer.md)  
> - [AI Entegrasyon](file:///c:/Users/kaya/projects/EntApp.Framework/docs/ai-integration-strategy.md)  
> - [Dinamik UI](file:///c:/Users/kaya/projects/EntApp.Framework/docs/dynamic-ui-generation.md)  
> **İlke:** Küçük fazlar, her faz kendi başına çalışıp test edilebilir

---

## Faz 1 — Solution Yapısı, Docker Compose & Walking Skeleton ✅

> **Tamamlanma:** 2026-04-03

**Hedef:** Proje iskeletini oluştur, geliştirme ortamını ayağa kaldır, uçtan uca çalışan minimal bir API ile altyapıyı kanıtla.

- [x] .NET 9 (STS) solution oluştur (`EntApp.sln`)
- [x] `Directory.Build.props` — global paket versiyonları, C# 13, nullable enable
- [x] `.editorconfig` — kod formatlama kuralları
- [x] Klasör yapısı: `src/`, `tests/`, `database/`, `docs/`, `frontend/`
- [x] `docker-compose.yml` — PostgreSQL 16, Redis 7, RabbitMQ, Seq
- [x] `docker-compose.override.yml` — geliştirme ortamı ayarları (port, volume)
- [x] Docker Compose ile tüm servisleri ayağa kaldır ve test et
- [x] `.gitignore`, `README.md`
- [x] **Walking Skeleton:** Host/WebAPI projesi oluştur, minimal `Program.cs`, `/health` endpoint çalışır

**Çıktı:** `docker-compose up` ile tüm altyapı servisleri + boş API ayağa kalkar, `/health` çalışır.

---

## Faz 2 — Shared.Kernel ✅

> **Tamamlanma:** 2026-04-03

**Hedef:** Tüm modüllerin paylaştığı temel yapı taşlarını oluştur.

- [x] `Shared.Kernel` class library projesi
- [x] `BaseEntity.cs` — Id (Guid), CreatedAt, UpdatedAt, IsDeleted (soft delete), **RowVersion** (optimistic concurrency)
- [x] `AuditableEntity.cs` — CreatedBy, ModifiedBy (BaseEntity'den türer)
- [x] `AggregateRoot.cs` — `_domainEvents` listesi, `AddDomainEvent()`, `ClearDomainEvents()`
- [x] `ITenantEntity.cs` — TenantId interface
- [x] `IDomainEvent.cs` — marker interface (MediatR `INotification`)
- [x] `Result<T>` — IsSuccess, IsFailure, Value, Error, Errors
- [x] `Error.cs` — Code, Message, Type (Validation, NotFound, Conflict, Unauthorized)
- [x] **`StronglyTypedId.cs`** — `IEntityId` interface + `EntityId.New<T>()` / `From<T>()` factory
- [x] Value Objects: `Money`, `DateRange`, `Address`, `Email`, `PhoneNumber`
- [x] Enums: `Status`, `Priority`
- [x] **`ISpecification<T>`** — Specification Pattern base interface + `SpecificationEvaluator`
- [x] Exceptions: `DomainException`, `NotFoundException`, `ConflictException`
- [x] Unit testler (xUnit) — 51 test, tümü geçti

**Çıktı:** Tüm modüllerin referans edebileceği sıfır bağımlılıklı kernel paketi (sadece MediatR.Contracts).

---

## Faz 3 — Shared.Contracts & Shared.Infrastructure

**Hedef:** Modüller arası kontratları ve ortak altyapıyı oluştur.

### 3a — Shared.Contracts ✅
- [x] `Shared.Contracts` class library projesi
- [x] `IIntegrationEvent.cs` — modüller arası event kontratı + **IdempotencyKey** (Guid)
- [x] `ICurrentUser.cs` — UserId, UserName, Roles, Permissions
- [x] `ICurrentTenant.cs` — TenantId, TenantName
- [x] `IEventBus.cs` — `PublishAsync<T>(T @event)` abstraction
- [x] `IUnitOfWork.cs` — `SaveChangesAsync()`, `BeginTransactionAsync()`
- [x] Ortak DTO'lar: `UserInfoDto`, `TenantInfoDto`, `PagedResult<T>`, `PagedRequest`, `LookupDto`

### 3b — Shared.Infrastructure: Persistence ✅
- [x] `Shared.Infrastructure` class library projesi
- [x] `BaseDbContext.cs` — audit fields otomatik set, soft delete filter, tenant filter, domain event dispatch (pre-commit), **AsSplitQuery varsayılan** (TPT JOIN performansı)
- [x] `OutboxMessage.cs` entity + `OutboxProcessor.cs` — integration event'leri Outbox'tan publish
- [x] `ProcessedEventStore.cs` — idempotency: işlenmiş event'leri filtrele
- [x] **Soft Delete + Unique Index:** PostgreSQL partial index ile `WHERE is_deleted = false` unique constraint desteği
- [x] EF Core interceptors: `AuditableEntityInterceptor`, `SoftDeleteInterceptor`, `DomainEventDispatchInterceptor` (**iki aşamalı dispatch:** `IDomainEvent` = pre-commit, `IPostCommitDomainEvent` = post-commit)

### 3c — Shared.Infrastructure: Pipeline Behaviors ✅
- [x] `ValidationBehavior.cs` — FluentValidation entegrasyonu
- [x] `LoggingBehavior.cs` — request/response Serilog ile loglama
- [x] `PerformanceBehavior.cs` — >500ms süren sorgulara warning log
- [x] `TransactionBehavior.cs` — Command'lar için begin/commit/rollback + **`ITransactionless` opt-out** mekanizması
- [x] `CachingBehavior.cs` — `ICacheableQuery` marker ile IDistributedCache

### 3d — Shared.Infrastructure: EventBus & Cache ✅
- [x] `InMemoryEventBus.cs` — MediatR doğrudan publish (dev/test)
- [x] `OutboxEventBus.cs` — Outbox tablosuna yazma (production)
- [x] `ICacheService.cs`, `DistributedCacheService.cs`, `CacheKeyBuilder.cs`

### 3e — Shared.Infrastructure: Middleware ✅
- [x] `ExceptionHandlingMiddleware.cs` — global hata yakalama, **RFC 7807 ProblemDetails** standart format
- [x] `RequestLoggingMiddleware.cs` — HTTP request loglama
- [x] `TenantResolutionMiddleware.cs` — header/subdomain/claim'den tenant belirleme
- [x] `RateLimitingConfiguration.cs` — **ASP.NET Core Rate Limiter** (fixed/sliding window, IP partitioning)
- [x] `AuditMiddleware.cs` — hassas işlem loglama, **PII maskeleme** (KVKK/GDPR uyumu)

### 3f — Shared.Infrastructure: Auth & Health ✅
- [x] `KeycloakAuthenticationExtensions.cs` — Keycloak JWT doğrulama + realm_access role mapping
- [x] `HttpContextCurrentUser.cs` — ICurrentUser implementasyonu (JWT claims)
- [x] `HttpContextCurrentTenant.cs` — ICurrentTenant implementasyonu
- [x] `PermissionAuthorizationHandler.cs` — policy-based RBAC
- [x] `HasPermissionAttribute.cs` — endpoint dekoratörü
- [x] `PermissionAuthorizationPolicyProvider.cs` — dinamik permission policy üretici
- [x] `ModuleHealthCheckAdapter.cs` — modül bazlı health check

### 3g — Shared.Infrastructure: RealTime ✅
- [x] `EntAppHub.cs` — merkezi SignalR hub (JWT auth, group join/leave, connection tracking)
- [x] `EntityChangeNotifier.cs` — entity değişikliğini push et (dual-group: entity + list)
- [x] `UserConnectionTracker.cs` — in-memory kullanıcı bağlantı takibi (thread-safe, multi-tab)

**Çıktı:** Tüm cross-cutting concern'ler çalışır durumda, unit testler yeşil.

---

## Faz 4 — Host/WebAPI & Composition Root ✅

**Hedef:** Walking Skeleton'ı tam işlevsel hale getir.

- [x] `Program.cs` — composition root (DI, middleware, Keycloak auth, Serilog, Swagger/Scalar)
- [x] `ModuleRegistration.cs` — **IModuleInstaller convention-based auto-discovery** (assembly taraması ile otomatik kayıt)
- [x] `IModuleInstaller.cs` — modül DI kurulum kontratı (Shared.Contracts)
- [x] `appsettings.json` — PostgreSQL, Redis, RabbitMQ, Keycloak, Seq bağlantı bilgileri
- [x] `appsettings.Development.json` — geliştirme ortamı override
- [x] Keycloak realm konfigürasyonu (realm, client, roles)
- [x] Swagger/Scalar endpoint çalışır durumda (JWT Bearer auth)
- [x] Serilog → Seq entegrasyonu konfigüre
- [x] `Dockerfile` — multi-stage build (non-root, healthcheck)
- [x] OpenTelemetry → Jaeger tracing (OTLP exporter, ASP.NET Core + HTTP instrumentation)
- [x] API versioning (`Asp.Versioning`) — URL segment + header reader
- [x] `DiagnosticsController.cs` — Walking Skeleton doğrulama (api/v1/diagnostics/ping + info)
- [x] **Migration stratejisi:** `MigrateDatabaseAsync<TContext>()` extension
- [x] **Seed data altyapısı:** `ISeedDataProvider` interface + `SeedDatabaseAsync()` extension

**Çıktı:** CRUD çalışan Walking Skeleton, migration + seed altyapısı hazır, loglar Seq'e gider.

---

## Faz 5 — Frontend Scaffold ✅

**Hedef:** Frontend iskeletini oluştur.

- [x] Next.js 16 projesi (App Router, TypeScript) — `src/Frontend/entapp-web`
- [x] Tailwind CSS 4 konfigürasyonu
- [x] `globals.css` — tema renkleri (dark/light), font (Inter), scrollbar, animasyonlar
- [x] `next-themes` — dark/light mode toggle
- [x] `providers.tsx` — ThemeProvider
- [x] Layout: sidebar (collapsible), header (user menu, notification bell, theme toggle)
- [x] Axios/fetch instance — base URL, JWT interceptor, refresh token
- [x] Zustand store: `useAuthStore`, `useUiStore`
- [x] Dashboard sayfası (stat cards + activity feed)
- [x] Keycloak login entegrasyonu (next-auth v5, OAuth2 PKCE, middleware, login page)
- [x] shadcn/ui bileşenleri (Button, Input, Dialog, Table, Select, Toast — 7 adet)
- [x] Orval Kurulumu (`orval.config.ts`, `npm run api:generate`)
- [x] **Test altyapısı:** Vitest v3 (7 test) + Playwright E2E scaffold
- [ ] **Tech Debt:** Next.js 16'da `middleware` dosya konvansiyonu deprecated — `proxy` konvansiyonuna geçiş yapılmalı ([detay](https://nextjs.org/docs/messages/middleware-to-proxy))

**Çıktı:** Login → sidebar'lı dashboard, dark/light mode çalışır.

---

## Faz 6 — IAM Modülü ✅

**Hedef:** Kimlik ve erişim yönetimi.

### 6a — IAM Domain & Application ✅
- [x] `IAM.Domain` — User, Role, Permission, Organization, Department entity'leri
- [x] `IAM.Application` — Commands: CreateUser, UpdateUser, AssignRole, DeactivateUser, CreateRole, CreateOrganization
- [x] `IAM.Application` — Queries: GetUserById, GetUsersPaged, GetRoles, GetOrganizationTree
- [x] FluentValidation validator'ları (5 adet)
- [x] Domain Events: `UserCreatedEvent`, `RoleAssignedEvent`, `UserDeactivatedEvent`

### 6b — IAM Infrastructure ✅
- [x] `IamDbContext.cs` — EF Core, PostgreSQL, kendi şeması (`iam.`)
- [x] `IamModuleInstaller.cs` — convention-based auto-discovery DI
- [x] Seed data: varsayılan roller (Admin, Manager, User, ReadOnly) + 6 IAM permission

### 6c — IAM API ✅
- [x] Controllers: UserController, RoleController, OrganizationController
- [x] Command Handlers: CreateUser, UpdateUser, AssignRole, DeactivateUser, CreateRole, CreateOrganization
- [x] Query Handlers: GetUserById, GetUsersPaged, GetRoles, GetOrganizationTree

**Çıktı:** Kullanıcı CRUD, rol/yetki yönetimi, Keycloak SSO çalışır.

---

## Faz 7 — Diğer Core Modüller

### 7a — Audit Modülü ✅
- [x] AuditLog, LoginRecord entity'leri (multi-tenant, jsonb old/new values)
- [x] AuditDbContext (`audit.` schema, performance indexes)
- [x] EF Interceptor (`AuditSaveChangesInterceptor`) — otomatik entity change tracking
- [x] AuditModuleInstaller — convention DI + MediatR auto-register
- [x] Query Handlers: GetAuditLogs, GetLoginRecords (paginated + multi-field filter)
- [x] AuditController — `GET /api/v1/audit/logs`, `GET /api/v1/audit/logins`

### 7b — Configuration Modülü ✅
- [x] AppSetting (key-value) + FeatureFlag entity'leri (tenant/global, scheduled, role-based)
- [x] Tenant/global bazlı konfigürasyon desteği (tenant > global fallback)
- [x] Feature flag açma/kapama/toggle API + zamanlama
- [x] Bakım modu mekanizması (FeatureFlag "MaintenanceMode")
- [x] ConfigController — 7 API endpoint
- [x] 34 unit test (AppSetting: 10, FeatureFlag: 14, Validators: 8)

### 7c — Notification Modülü ✅
- [x] NotificationTemplate, NotificationLog, UserNotificationPreference entity'leri
- [x] Provider'lar: SMTP e-posta (placeholder), InApp bildirim sender
- [x] SimpleTemplateRenderer — `{{variable}}` regex substitution (Scriban CVE nedeniyle built-in)
- [x] Kullanıcı bildirim tercihleri (kanal bazlı opt-in/opt-out, upsert)
- [x] Bildirim geçmişi (paginated) ve okunma takibi (MarkRead + UnreadCount)
- [x] NotificationController — 9 API endpoint (templates CRUD, send, history, preferences)
- [x] 24 unit test (Template: 6, Log: 5, Preference: 4, Validator: 8, auto: 1)

### 7d — FileManagement Modülü ✅
- [x] FileEntry (soft delete, versioning, tagging, preview), FileVersion, FileTag entity'leri
- [x] Storage provider abstraction: LocalDisk + MinIO/S3 (config-driven switch)
- [x] Dosya yükleme/indirme API (100MB limit, IFormFile)
- [x] Dosya önizleme (pdf, png, jpg, webp, svg, gif, bmp)
- [x] Versiyon yönetimi, metadata güncelleme, tag CRUD, soft delete + geri yükleme
- [x] FileController — 11 API endpoint
- [x] 26 unit test (FileEntry: 15, FileTag: 2, Validators: 8, auto: 1)

### 7e — MultiTenancy Modülü (UI & Yönetim) ✅

> [!NOTE]
> MultiTenancy **altyapısı** (ITenantEntity, TenantResolutionMiddleware, EF global filter) Faz 3'te zaten kurulmuştur. Bu fazda UI ve yönetim katmanı eklendi.

- [x] Tenant (status lifecycle, plan, subdomain, connection string), TenantSetting entity'leri
- [x] Tenant CRUD API — 11 endpoint (list, get by id/identifier, create, update, activate/suspend/deactivate, plan, subdomain, settings)
- [x] **Tenant Bootstrapper:** `ITenantSeeder` interface — yeni tenant oluşturulduğunda modüllerin seed çalıştırması
- [x] TenantController — 11 API endpoint
- [x] 25 unit test (Tenant: 14, TenantSetting: 3, Validators: 7, auto: 1)

### 7f — Localization Modülü ✅
- [x] Language (code, name, nativeName, default, active, displayOrder), TranslationEntry (namespace.key, verified, tenant-specific override, audit) entity'leri
- [x] Dinamik çeviri yönetimi (DB'den) — upsert, bulk upsert, verify, delete
- [x] API: 10 endpoint — diller (list, create, set-default, toggle), çeviriler (get, map, by-key, upsert, bulk, verify, delete)
- [x] Frontend desteği: `GET /translations/{lang}/map` — flat JSON map (tenant override destekli, AllowAnonymous)
- [x] 24 unit test (Language: 8, TranslationEntry: 8, Validators: 7, auto: 1)

**Çıktı:** Tüm core modüller çalışır, test edilmiş, frontend sayfaları mevcut.

---

## Faz 8 — Dynamic UI Engine

### 8a — Backend Metadata Engine ✅

> **Tamamlanma:** 2026-04-04

- [x] `DynamicEntityAttribute`, `DynamicFieldAttribute`, `DynamicLookupAttribute`, `DynamicDetailAttribute`, `FieldType` enum — `Shared.Kernel/Domain/Attributes/`
- [x] `MetadataService.cs` — reflection + convention-based fallback ile entity'den JSON schema üretimi
- [x] `DynamicCrudEndpointBuilder.cs` — `app.MapDynamicCrudEndpoints()` minimal API endpoint registration (`/api/v1/dynamic/{entity}`)
- [x] Menu endpoint — `GET /api/v1/dynamic/meta/menu` otomatik sidebar menu üretimi
- [x] Generic CRUD service: `DynamicCrudService.cs` — EF Core reflection tabanlı GetPaged, GetById, Create, Update, Delete
- [x] Lookup endpoint: `GET /api/v1/dynamic/{entity}/lookup?search=abc`
- [x] `DynamicEntityRegistry` — assembly scan ile entity keşfi
- [x] `DynamicDbContextProvider` — entity → DbContext runtime eşleştirmesi
- [x] DI extensions: `AddDynamicCrud()`, `AddDynamicDbContext<T>()`
- [x] Host entegrasyonu: `Program.cs`

### 8b — Frontend Render Engine ✅ (MVP)

> **Tamamlanma:** 2026-04-04 (MVP)

- [x] `DynamicPage.tsx` — orchestrator component
- [x] `DynamicTable.tsx` — metadata'dan otomatik kolon, sort, pagination, row actions
- [x] `DynamicForm.tsx` — React Hook Form + Zod (metadata'dan runtime schema), sheet dialog
- [x] `DynamicField.tsx` — field type → component router (Input, Select, DatePicker, Switch, money prefix)
- [x] `DynamicToolbar.tsx` — arama, yenile, yeni ekle
- [x] `useDynamicMeta.ts` + `useDynamicMenu.ts` hooks — metadata + menu fetch + TanStack Query cache
- [x] `useDynamicCrud.ts` hook — list, getById, create, update, delete, lookup mutations
- [x] `schema-to-zod.ts` — metadata → Zod validation schema üretimi
- [x] Otomatik sidebar menu (meta/menu endpoint'inden, dinamik bölüm)
- [x] Dynamic route: `/dashboard/dynamic/[entityName]`
- [x] `QueryClientProvider` wiring (`providers.tsx`)
- [x] `DynamicLookup.tsx` — async arama destekli combobox *(Faz 8e'de implemente edildi)*
- [x] `DynamicFilters.tsx` — gelişmiş filtreleme paneli *(Faz 8e'de implemente edildi)*
- [ ] `DynamicDetailTable.tsx` — master-detail alt tablo *(→ Faz 11, SalesOrder/OrderItem gerekli)*
- [ ] Override mekanizmaları: config, fieldOverrides, detailOverrides *(→ Faz 13)*

### 8c — Import/Export Engine ✅

> **Tamamlanma:** 2026-04-04

- [x] `DynamicExportService.cs` — entity → Excel (ClosedXML), CSV (CsvHelper)
- [x] `DynamicImportService.cs` — Excel/CSV parse + validate + bulk insert
- [x] `ColumnMapper.cs` — Excel kolon → entity field otomatik eşleştirme
- [x] `ExportTemplateBuilder.cs` — boş import şablonu üretimi (hint row dahil)
- [x] `GET /api/v1/dynamic/{entity}/export?format=xlsx|csv`
- [x] `GET /api/v1/dynamic/{entity}/import-template`
- [x] `POST /api/v1/dynamic/{entity}/import/preview`
- [x] `POST /api/v1/dynamic/{entity}/import`
- [x] Frontend: `DynamicImport.tsx` — 3 adımlı wizard (dosya yükle → kolon eşle → sonuç)
- [x] Frontend: `DynamicExport.tsx` — format seçimi dialogu + şablon indirme
- [x] Toolbar entegrasyonu (Dışa Aktar + İçe Aktar butonları)

### 8d — Real-time Entegrasyon ✅

> **Tamamlanma:** 2026-04-04

- [x] `DynamicCrudEndpointBuilder` → Create/Update/Delete → `IEntityChangeNotifier` push
- [x] `EntAppHub` anonymous erişim (dev)
- [x] `useSignalR.ts` hook — connect, JoinGroup, EntityChanged listener
- [x] `DynamicTable` — yeni kayıt: yeşil highlight, silme: fade-out animasyonu
- [x] `next.config.ts` — `/hubs/**` WebSocket proxy

### 8e — Test & Demo ✅

> **Tamamlanma:** 2026-04-04

> [!IMPORTANT]
> **DynamicDetailTable.tsx** Faz 11'e (Business Framework) taşındı — SalesOrder/OrderItem entity'leri orada oluşturulacak.

- [x] Lookup entity'ler: Country, City (→Country FK), Currency (`[DynamicEntity]` + "Tanımlar")
- [x] `ConfigDbContext` güncelleme (DbSet + FK + QueryFilter + Index)
- [x] `Program.cs` wiring (AddDynamicCrud + AddDynamicDbContext)
- [x] `DynamicLookup.tsx` — async arama destekli combobox
- [x] `DynamicField.tsx` → lookup routing entegrasyonu
- [x] `DynamicFilters.tsx` — collapsible filtreleme paneli (boolean/enum/lookup/text)
- [x] `DynamicPage.tsx` → filtre entegrasyonu

**Çıktı:** `[DynamicEntity]` attribute ekle → otomatik CRUD ekranı, lookup FK, filtreleme, export/import.

---

## Faz 9 — AI Module

### 9a — AI Altyapısı ✅

> **Tamamlanma:** 2026-04-04

- [x] `AI.Domain` — AiModel, PromptTemplate, EmbeddingDocument, AiUsageLog entity'leri
- [x] `AI.Application` — ILlmService, IEmbeddingService, IRagService, IPromptManager, IDocumentProcessor interfaces
- [x] Microsoft Semantic Kernel entegrasyonu
- [x] Multi-provider: config-driven OpenAI / AzureOpenAI seçimi, Anthropic / Ollama hazır
- [x] `appsettings.json` ile provider seçimi (`AiSettings:DefaultProvider`)
- [x] `SemanticKernelLlmService` — chat completion + AiUsageLog maliyet takibi
- [x] `ScribanPromptManager` — DB'den template + Scriban render

### 9b — Embedding & Vector Search ✅

> **Tamamlanma:** 2026-04-04

- [x] PostgreSQL pgvector extension kurulumu (`pgvector/pgvector:pg16`, v0.8.2)
- [x] `PgVectorStore.cs` — embedding CRUD + cosine similarity search (HNSW index)
- [x] `EmbeddingDocument` tablosu: tenant_id, module_name, content, embedding(`vector(1536)`), metadata
- [x] Embedding API: `POST /api/ai/embed`, `POST /api/ai/search`, `POST /api/ai/store`
- [x] `SemanticKernelEmbeddingService` — `IEmbeddingGenerator` impl + usage logging

### 9c — RAG Pipeline ✅

> **Tamamlanma:** 2026-04-04

- [x] `DocumentProcessor` — PDF (PdfPig), TXT, MD → düz metin çıkarma
- [x] `TextChunker` — paragraf/cümle bazlı chunking (500 token, 50 overlap)
- [x] Chunk → embedding → pgvector'e kaydet (`POST /api/ai/ingest`)
- [x] RAG: query embedding → benzer chunk bul → LLM'e context olarak ver → yanıt üret
- [x] `POST /api/ai/rag` endpoint
- [x] `POST /api/ai/ingest` endpoint (dosya yükleme)

### 9d — Prompt Management ✅

> **Tamamlanma:** 2026-04-04

- [x] PromptTemplate CRUD API (`/api/ai/prompts`)
- [x] Versiyonlama (aynı key, otomatik versiyon artışı)
- [x] Şablon engine (Scriban) — `{{variable}}` desteği
- [x] Render/test endpoint'leri (DB'den + inline)
- [ ] Frontend: prompt düzenleme ekranı (Admin Panel'e entegre) — sonraki fazda

### 9e — Maliyet Kontrolü ✅

> **Tamamlanma:** 2026-04-04

- [x] `AiUsageLog` — her LLM/Embedding çağrısı: modül, token sayısı, maliyet, süre
- [x] Usage Dashboard API: `/api/ai/usage/summary`, `/daily`, `/logs`
- [x] Rate limiting — `AiRateLimiter` (sliding window, tenant/modül bazlı, configurable)
- [x] Model routing — `ModelRouter` (basit iş → LiteModel, karmaşık → ChatModel)
- [x] LLM response caching — `AiResponseCache` (IDistributedCache/Redis)

**Çıktı:** LLM abstraction, embedding/search, RAG, prompt yönetimi, maliyet kontrolü çalışır.

---

## Faz 10 — Workflow & Approval Engine ✅

> **Tamamlanma:** 2026-04-04

- [x] Hafif onay motoru (Elsa yerine kendi state machine çözümü)
- [x] `Workflow.Domain` — WorkflowDefinition, WorkflowInstance, ApprovalStep entity'leri
- [x] `Workflow.Application` — IWorkflowEngine interface
- [x] `WorkflowEngine` — state machine (Sequential, Parallel, AnyOne) + eskalasyon
- [x] `WorkflowDbContext` — schema: `wf`, 3 tablo, 4 index
- [x] Basit onay akışı tanımlama API (sıralı, paralel, any-one)
- [x] Yetki bazlı onay zinciri (userId/role assignment)
- [x] Zaman aşımı (dueDate) ve eskalasyon desteği
- [x] Workflow geçmişi ve takip (`/api/workflows/{id}`)
- [x] 13 API endpoint (definition CRUD, start, approve, reject, escalate, cancel, pending)
- [ ] Dinamik form desteği — sonraki fazda
- [ ] Frontend: onay bekleyen görevler listesi — sonraki fazda
- [ ] `ApprovalCompletedIntegrationEvent` — sonraki fazda

**Çıktı:** Basit onay akışları oluşturulup çalıştırılabilir.

---

## Faz 11 — Business Framework: Tier 1 Modüller

**Hedef:** En çok kullanılan iş modüllerinin generic core'unu oluştur.

### 11a — CRM ✅

> **Tamamlanma:** 2026-04-04

- [x] `CustomerBase`, `ContactBase`, `OpportunityBase`, `ActivityBase` entity'leri
- [x] Temel CRUD + arama/filtreleme (14 API endpoint)
- [x] Müşteri segmentasyonu (Standard → Enterprise)
- [x] Fırsat aşama geçişi (Lead → ClosedWon/Lost pipeline)
- [x] Pipeline özet endpoint'i (`/api/crm/opportunities/pipeline`)
- [x] `CrmDbContext` — schema: `crm`, 4 tablo, 7 index
- [ ] Integration Events — sonraki fazda
- [ ] Dynamic UI ile ekranlar — sonraki fazda

### 11b — HR ✅

> **Tamamlanma:** 2026-04-04

- [x] `EmployeeBase`, `LeaveRequestBase`, `AttendanceBase` entity'leri
- [x] Personel CRUD + arama/filtreleme (14 API endpoint)
- [x] Organizasyon şeması (self-referencing ManagerId + org-chart endpoint)
- [x] İzin talebi + onay/red + bakiye kontrolü
- [x] Puantaj kaydı (giriş/çıkış, çalışma saati, fazla mesai otomatik hesaplama)
- [x] `HrDbContext` — schema: `hr`, 3 tablo, 4 index
- [x] Workflow entegrasyonu hazır (WorkflowInstanceId alanı)
- [ ] Integration Events — sonraki fazda

### 11c — Finance ✅

> **Tamamlanma:** 2026-04-04

- [x] `AccountBase`, `InvoiceBase`, `InvoiceItemBase`, `PaymentBase` entity'leri
- [x] Cari hesap yönetimi + bakiye hesaplama (otomatik güncelleme)
- [x] Fatura oluşturma (satış/satın alma) + kalem bazlı KDV/indirim
- [x] Ödeme kaydı + faturaya otomatik ödeme bağlama
- [x] Vade takibi: vadesi geçmiş fatura endpoint'i
- [x] Bakiye özeti: hesap tipi bazlı toplamlar
- [x] `FinanceDbContext` — schema: `fin`, 4 tablo, 7 index
- [ ] Integration Events — sonraki fazda

### 11d — Inventory ✅

> **Tamamlanma:** 2026-04-04

- [x] `ProductBase`, `WarehouseBase`, `StockMovementBase` entity'leri
- [x] Ürün CRUD, barkod/SKU (arama desteği)
- [x] Stok giriş/çıkış/transfer/sayım düzeltme/iade
- [x] Depo bazlı stok bakiyesi endpoint'i
- [x] Min/max stok + reorder uyarı endpoint'i
- [x] `InventoryDbContext` — schema: `inv`, 3 tablo, 5 index

**Çıktı:** CRM, HR, Finance, Inventory generic core modülleri çalışır ✅

---

## Faz 12 — Business Framework: Tier 2 Modüller

### 12a — Sales ✅

> **Tamamlanma:** 2026-04-04

- [x] `SalesOrderBase`, `OrderItemBase`, `PriceListBase` entity'leri
- [x] Sipariş oluşturma + kalem bazlı fiyat/iskonto (%/sabit)
- [x] Fiyat listesi yönetimi (Standard/Wholesale/Dealer/Campaign)
- [x] Sipariş durumu geçişi (Draft → Confirmed → Processing → Shipped → Delivered)
- [x] Workflow entegrasyonu hazır (WorkflowInstanceId)
- [x] Satış dashboard endpoint'i (günlük/aylık gelir, durum bazlı sayılar)
- [x] `SalesDbContext` — schema: `sales`, 3 tablo, 4 index
- [ ] Inventory ile stok düşümü (Integration Event) — sonraki fazda
- [ ] Finance ile fatura oluşturma (Integration Event) — sonraki fazda

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

> [!IMPORTANT]
> **Faz 8b'den ertelenen:** Override mekanizmaları (config, fieldOverrides, detailOverrides) bu fazda `DynamicUIConfigs` DB tablosu ile birlikte implemente edilecek.

- [ ] Admin Panel modülü (ayrı frontend route: `/admin`)
- [ ] **Tenant Yönetimi:** tenant CRUD, konfigürasyon, deaktif etme
- [ ] **Feature Flags:** modül/tenant bazlı açma/kapama toggle'ları
- [ ] **Prompt Yönetimi:** AI prompt şablonları düzenleme, versiyon karşılaştırma
- [ ] **Background Jobs:** Hangfire dashboard embed
- [ ] **AI İstatistikleri:** token kullanımı, maliyet raporu (Recharts grafikleri)
- [ ] **System Health:** modül bazlı health check durumu, DB/Redis/RabbitMQ bağlantısı
- [ ] **Audit Viewer:** kullanıcı aktivite logları, entity change history tarayıcısı
- [ ] **Cache Yönetimi:** Redis cache temizleme, cache hit/miss oranları\r
- [ ] **DynamicUIConfigs DB Tablosu:** Dynamic UI entity/field konfigürasyonları (label, order, width, showInList, icon) — convention-based fallback yerine admin panelden yönetim

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
| 1 | Solution & Docker Compose & Walking Skeleton | 1-2 gün |
| 2 | Shared.Kernel (+ StronglyTypedId, RowVersion, Specification) | 2-3 gün |
| 3 | Shared.Contracts & Infrastructure (+ MultiTenancy altyapısı, RFC 7807, Rate Limiting) | 1-2 hafta |
| 4 | Host/WebAPI (+ Migration stratejisi, Seed altyapısı) | 2-3 gün |
| 5 | Frontend Scaffold (+ Vitest/Playwright, Orval) | 3-4 gün |
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

---

## ❌ Not Planned

Aşağıdaki özellikler değerlendirilmiş ancak çeşitli nedenlerle kapsam dışı bırakılmıştır.

| Özellik | Neden | Alternatif |
|---------|-------|-----------|
| **PDF Export (QuestPDF)** | Community lisansı ticari kullanımda kısıtlı (yıllık gelir >1M$ → ücretli). Layout karmaşıklığı MVP için gereksiz. | Excel export yeterli. İhtiyaç doğarsa iTextSharp veya wkhtmltopdf değerlendirilebilir. |

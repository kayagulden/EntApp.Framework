# 16d -- Test Management -- Detayli Yol Haritasi

> **Ana roadmap:** [-roadmap.md (Faz 16)]
> **Bagimliliklar:** 16c (Requirements), mevcut WorkItem altyapisi, StateFlow Engine
> **Durum:** Faz 1 MVP tamamlandi ✅ (2026-04-24)

---

## Tasarim Kararlari

> [!IMPORTANT]
> **Modul Konumu:** Test Management entity'leri TaskManagement modulu icinde yasar.
> Ayri bir modul olusturmaya gerek yok -- TestScenario, Requirement ve WorkItem
> ayni DbContext'i (pm schema) paylasir, FK iliskileri dogrudan kurulur.

> [!NOTE]
> **Terminoloji:** TestCase yerine **TestScenario** kullanilir.
> Bir senaryo = bir test akisi (birden fazla adim). Parametrize testler icin
> ayni senaryo farkli TestDataSet'lerle calistirilir.

> [!NOTE]
> **Numaralama:** Test senaryolari proje bazli KEY-TC1, KEY-TC2 seklinde numaralanir.
> TestPlan'lar KEY-TP1, KEY-TP2 seklinde numaralanir.
> (Mevcut pattern: WorkItem = KEY-WI-N, Requirement = KEY-RN)

> [!IMPORTANT]
> **Manuel vs Otomasyon:** MVP'de yalnizca **manuel test** yonetimi.
> Otomasyon entegrasyonu (CI/CD pipeline'dan sonuc aktarma) Faz 3'te.

---

## Faz 1 — MVP ✅ (Tamamlandi: 2026-04-24)

### Entity: TestScenario (Test Senaryosu)
- [x] Id: TestScenarioId (strongly-typed)
- [x] ProjectId: ProjectId (FK)
- [x] Key: string (KEY-TC1)
- [x] Title, Description (Markdown), Preconditions (Markdown)
- [x] Type: TestScenarioType (Functional, Regression, Smoke, Integration, UAT, Performance)
- [x] Priority: TestScenarioPriority (Critical, High, Medium, Low)
- [x] Status: TestScenarioStatus (Draft, Active, Deprecated, Archived)
- [x] RequirementId: RequirementId? (FK -- hangi gereksinimden turetildi)
- [x] EstimatedDuration: TimeSpan? (tahmini calistirma suresi)
- [x] Tags: string? (virgulle ayrilmis etiketler)
- [x] SortOrder: int
- [x] Steps: List TestStep (navigation)

### Entity: TestStep (Senaryo Adimi)
- [x] Id: TestStepId (strongly-typed)
- [x] TestScenarioId: TestScenarioId (FK)
- [x] StepNumber: int (sira: 1, 2, 3...)
- [x] Action: string (Yapilacak islem)
- [x] ExpectedResult: string (Beklenen sonuc)
- [x] TestData: string? (Test verisi)
- [x] Notes: string?

### Entity: TestPlan (Test Plani)
- [x] Id: TestPlanId (strongly-typed)
- [x] ProjectId: ProjectId (FK)
- [x] Key: string (KEY-TP1)
- [x] Title, Description
- [x] Status: TestPlanStatus (Draft, Active, InExecution, Completed, Cancelled)
- [x] SprintId: SprintId? (FK)
- [x] MilestoneId: MilestoneId? (FK)
- [x] StartDate, EndDate: DateOnly?
- [x] AssignedTesterId: string?

### Entity: TestPlanScenario (Plan-Senaryo M:N)
- [x] Id: Guid
- [x] TestPlanId: TestPlanId (FK)
- [x] TestScenarioId: TestScenarioId (FK)
- [x] AssignedTesterId: string?
- [x] SortOrder: int

### Entity: TestExecution (Test Calistirma Kaydi)
- [x] Id: TestExecutionId (strongly-typed)
- [x] TestPlanScenarioId: Guid (FK)
- [x] ExecutedBy: string (userId)
- [x] ExecutedAt: DateTime
- [x] Result: TestResult (Pass, Fail, Blocked, Skipped, NotRun)
- [x] Duration: TimeSpan?
- [x] Notes: string?
- [x] Environment: string?
- [x] LinkedBugId: WorkItemId? (FK -- Fail'da olusturulan Bug)

### Entity: TestStepResult (Adim Bazli Sonuc)
- [x] Id: Guid
- [x] TestExecutionId: TestExecutionId (FK)
- [x] TestStepId: TestStepId (FK)
- [x] Result: TestResult (Pass, Fail, Blocked, Skipped)
- [x] ActualResult: string?
- [x] Notes: string?

### Enum'lar
- [x] TestScenarioType: Functional, Regression, Smoke, Integration, UAT, Performance
- [x] TestScenarioPriority: Critical, High, Medium, Low
- [x] TestScenarioStatus: Draft, Active, Deprecated, Archived
- [x] TestPlanStatus: Draft, Active, InExecution, Completed, Cancelled
- [x] TestResult: Pass, Fail, Blocked, Skipped, NotRun

### CRUD API

#### Test Senaryolari
- [x] POST /api/pm/projects/{projectId}/test-scenarios
- [x] GET  /api/pm/projects/{projectId}/test-scenarios (filtreli)
- [x] GET  /api/pm/test-scenarios/{id} (adimlar dahil)
- [x] PUT  /api/pm/test-scenarios/{id}
- [x] DELETE /api/pm/test-scenarios/{id}
- [x] PUT /api/pm/test-scenarios/{id}/steps (toplu adim guncelleme)

#### Test Planlari
- [x] POST /api/pm/projects/{projectId}/test-plans
- [x] GET  /api/pm/projects/{projectId}/test-plans
- [x] GET  /api/pm/test-plans/{id} (senaryolar + son execution)
- [x] PUT  /api/pm/test-plans/{id}
- [x] DELETE /api/pm/test-plans/{id}
- [x] POST /api/pm/test-plans/{planId}/scenarios (senaryo ekle)
- [x] DELETE /api/pm/test-plans/{planId}/scenarios/{scenarioId}

#### Test Calistirma
- [x] POST /api/pm/test-plans/{planId}/scenarios/{scenarioId}/execute
- [x] GET  /api/pm/test-plans/{planId}/executions
- [x] GET  /api/pm/test-scenarios/{id}/executions

### Frontend UI

#### Test Senaryolari Tab (Proje Detay)
- [x] Senaryo listesi (tip, oncelik, durum badge, bagli gereksinim)
- [x] Senaryo olusturma/duzenleme formu
- [x] Adim ekleme/duzenleme (sirali)
- [ ] Adim drag-and-drop siralama
- [ ] Gereksinim baglantisi (dropdown)

#### Test Planlari Tab (Proje Detay)
- [x] Plan listesi (durum, ilerleme cubugu)
- [x] Plan detay -- senaryolar ve son calistirma sonuclari
- [x] Plana senaryo atama
- [ ] Plana senaryo atama -- checkbox ile coklu secim

#### Test Calistirma Ekrani
- [x] Genel sonuc kaydetme (Pass/Fail/Blocked/Skipped + not)
- [ ] Step-by-step calistirma UI (her adim icin ayri Pass/Fail + not)
- [ ] Fail durumunda Bug Olustur butonu

### Database
- [x] pm.test_scenarios tablosu
- [x] pm.test_steps tablosu
- [x] pm.test_plans tablosu
- [x] pm.test_plan_scenarios tablosu
- [x] pm.test_executions tablosu
- [x] pm.test_step_results tablosu
- [x] pm.projects: TestScenarioSequence, TestPlanSequence sayaclari
- [x] Migration SQL: migrations/20260424_test_management.sql

### Dogrulama ✅
- [x] dotnet build -- 0 hata, 0 uyari
- [x] Test senaryo CRUD API testi (KBN-TC1, KBN-TC2)
- [x] Test plan + senaryo atama + execution testi (KBN-TP1)
- [x] Frontend: Test Senaryolari tab -- liste, form, adim duzenleme
- [x] Frontend: Test Planlari tab -- plan liste, senaryo atama

### Bilinen Sorunlar (Cozuldu)
- [x] `ModifiedBy` vs `UpdatedBy` kolon adi uyumsuzlugu (AuditableEntity `ModifiedBy` kullanir)
- [x] `test_plan_scenarios` ve `test_step_results` tablolarinda eksik BaseEntity kolonlari
- [x] Frontend API URL portu (5104 -> 5212)

---

## Faz 1.5 — MVP Iyilestirmeler (Sonraki Sprint)

### Frontend Gelistirmeleri
- [ ] Adim drag-and-drop siralama (react-beautiful-dnd veya dnd-kit)
- [ ] Gereksinim baglantisi dropdown (mevcut gereksinimlerden secim)
- [ ] Plana coklu senaryo atama (checkbox ile)
- [ ] Step-by-step calistirma UI (her adim icin ayri sonuc)
- [ ] Test filtre ve arama iyilestirmeleri (tip, oncelik, durum filtreleri kombine)
- [ ] Senaryo detay paneli -- execution gecmisi timeline
- [ ] Export: Senaryo ve plan verilerini Excel/CSV olarak indirme

### Otomatik Bug Olusturma
- [ ] Fail sonucunda Bug Olustur aksiyonu
- [ ] WorkItem Bug otomatik doldurma (Title, Description, RequirementId)
- [ ] Fail adim bilgisi ve environment detaylarini Bug description'a ekleme

### API Gelistirmeleri
- [ ] Toplu senaryo durum guncelleme (bulk status update)
- [ ] Senaryo klonlama (mevcutu kopyalayarak yeni olusturma)
- [ ] Plan klonlama (ayni senaryolarla yeni plan)
- [ ] Senaryo arama -- full-text search

---

## Faz 2 -- Coverage ve Reporting

### Test Coverage Raporlari
- [ ] Gereksinim bazli coverage (kac senaryo, kaci Pass)
- [ ] Sprint bazli coverage
- [ ] Plan bazli dashboard (Pass/Fail/Blocked/NotRun dagilimi)
- [ ] Coverage dashboard widget

### Traceability Matrix (16c ile birlikte)
- [ ] Gereksinim -> WorkItem -> TestScenario -> TestExecution -> Release matrisi
- [ ] Eksik coverage uyarisi
- [ ] Heatmap: En cok Fail olan gereksinimler/senaryolar

### Test Veri Yonetimi
- [ ] TestDataSet entity (parametrize test verileri)
- [ ] Ayni senaryo farkli veri setleriyle calistirma
- [ ] Veri seti sablonlari (JSON format)

---

## Faz 3 -- Otomasyon Entegrasyonu

### 3a -- Sonuc Import Altyapisi
- [ ] JUnit XML parser (pytest, unittest, Robot Framework uyumlu)
- [ ] POST /api/pm/test-results/import endpoint'i
- [ ] TestScenario key eslestirme (marker bazli: @pytest.mark.tc(KEY-TC1))
- [ ] Eslestirilemeyenler -> Unlinked Results olarak kayit
- [ ] TestExecution.Source enum: Manual, Automated, Pipeline

### 3b -- Azure DevOps Entegrasyonu
- [ ] Baglanti ayarlari (Tenant bazli): URL, PAT/OAuth, Organization, Project
- [ ] Azure DevOps Cloud destegi (dev.azure.com)
- [ ] Azure DevOps On-Prem (TFS/Azure DevOps Server) destegi
- [ ] Service Hook webhook receiver (Build Completed event)
- [ ] Alternatif: Polling ile test run sonuclari cekme
- [ ] Azure DevOps Test Run -> EntApp TestExecution eslestirme

### 3c -- GitHub Entegrasyonu
- [ ] Baglanti ayarlari (Tenant bazli): GitHub App veya PAT
- [ ] GitHub Actions workflow webhook receiver
- [ ] Artifact download -> JUnit XML parse
- [ ] GitHub Check Run -> EntApp TestExecution eslestirme

### 3d -- Python Test Projesi Rehberi
- [ ] pytest marker ekleme kilavuzu (@pytest.mark.tc dekoratoru)
- [ ] conftest.py sablonu (otomatik raporlama)
- [ ] Pipeline template ornekleri (Azure DevOps YAML, GitHub Actions YAML)
- [ ] Robot Framework tag destegi

### 3e -- Ek Dosya / Screenshot Destegi
- [ ] TestExecution'a dosya ekleme (screenshot, video, log)
- [ ] FileStorage (MinIO) entegrasyonu
- [ ] Fail durumunda otomatik screenshot referansi

### 3f -- Ileri Raporlama
- [ ] Test execution trend raporu (son 10 plan zaman serisi)
- [ ] Flaky test tespiti (bazen Pass bazen Fail senaryolar)
- [ ] Test execution suresi analizi

---

## Implementasyon Dosya Haritasi

| Katman | Dosya | Durum |
|--------|-------|-------|
| Domain — IDs | `TaskManagement.Domain/Ids/TaskManagementIds.cs` | ✅ |
| Domain — Enums | `TaskManagement.Domain/Enums/TaskManagementEnums.cs` | ✅ |
| Domain — Entities | `TaskManagement.Domain/Entities/TestScenario.cs` | ✅ |
| Domain — Entities | `TaskManagement.Domain/Entities/TestStep.cs` | ✅ |
| Domain — Entities | `TaskManagement.Domain/Entities/TestPlan.cs` | ✅ |
| Domain — Entities | `TaskManagement.Domain/Entities/TestPlanScenario.cs` | ✅ |
| Domain — Entities | `TaskManagement.Domain/Entities/TestExecution.cs` | ✅ |
| Domain — Entities | `TaskManagement.Domain/Entities/TestStepResult.cs` | ✅ |
| Domain — Entities | `TaskManagement.Domain/Entities/ProjectBase.cs` (sequence) | ✅ |
| Application — Commands | `TaskManagement.Application/Commands/TaskManagementCommands.cs` | ✅ |
| Application — Queries | `TaskManagement.Application/Queries/TaskManagementQueries.cs` | ✅ |
| Infrastructure — DbContext | `TaskManagement.Infrastructure/Persistence/TaskManagementDbContext.cs` | ✅ |
| Infrastructure — Handlers | `TaskManagement.Infrastructure/Handlers/TaskManagementHandlers.cs` | ✅ |
| Infrastructure — Endpoints | `TaskManagement.Infrastructure/Endpoints/TaskManagementEndpoints.cs` | ✅ |
| Database | `migrations/20260424_test_management.sql` | ✅ |
| Frontend | `projects/[id]/TestScenariosTab.tsx` | ✅ |
| Frontend | `projects/[id]/TestPlansTab.tsx` | ✅ |
| Frontend | `projects/[id]/page.tsx` (tab config + imports) | ✅ |

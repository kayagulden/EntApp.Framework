# Unified Delivery Platform — Modül Detayları

> **Tarih:** 2026-03-28  
> **Temel:** EntApp.Framework (Technical Framework + Core Modüller)  
> **Durum:** Tasarım tartışması  
> **İsim Önerileri:** DeliverHub · ReqFlow · PlanForge · WorkStream · LaunchPad

---

## Modüller Arası Traceability Zinciri

Bu projenin en kritik özelliği — her şeyin birbirine bağlanması:

```
Ticket/Talep
  └─► Proje (talep bir projeye dönüşür veya mevcut projeye bağlanır)
       └─► Gereksinimler (FR, NFR)
            └─► İş Kuralları (gereksinime bağlı)
            └─► Mockup'lar (gereksinime bağlı)
            └─► Backlog Item'lar (gereksinimden türer)
                 └─► Sprint'e atanır (Scrum) / Board'da akar (Kanban)
                      └─► Test Senaryoları (backlog item'a bağlı)
                           └─► Test Planı (senaryolar gruplanır)
                                └─► Test Execution (pass/fail/blocked)
                                     └─► Bug → yeni Backlog Item
                 └─► Release'e dahil edilir
                      └─► Go/No-Go → Deploy

Talep sahibi bu zincirin HER adımını portal'dan görebilir.
```

---

## Modül 1: Request Management (Talep Yönetimi)

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **RequestCategory** | Name, Code, Department, Description, ElsaWorkflowDefinitionId, FormSchema (Dynamic UI entegrasyonu), SlaDefinitionId, AutoProjectThreshold (efor eşiği) | → Department, → SlaDefinition, → Elsa Workflow |
| **Ticket** | Number, Title, Description, Priority, Status (Elsa'dan gelir), SlaDeadline, Channel (Email/Portal/Phone) | → RequestCategory, → Reporter, → Assignee, → Department, → Project (opsiyonel) |
| **TicketComment** | Content, IsInternal (müşteri göremez), Attachments | → Ticket |
| **TicketStatusHistory** | OldStatus, NewStatus, ChangedBy, ChangedAt, Reason | → Ticket |
| **SlaDefinition** | Name, Priority→ResponseTime, Priority→ResolutionTime | → RequestCategory (1:N) |
| **Department** | Name, Code, Manager | → Parent Department, → RequestCategories (1:N) |

### Elsa Workflow ile Konfigüre Edilebilir Akışlar

Her `RequestCategory` bir Elsa workflow tanımına bağlıdır. Admin panelden Elsa designer ile akış tasarlanır:

```
Departman seç → Talep Kategorisi seç → İlgili form açılır → Ticket oluşturulur → Elsa akışı başlar
```

> [!IMPORTANT]
> **Elsa State Machine modeli kullanılacak.** Flowchart modeli yalnızca ileri yönlü akışları destekler. State Machine modeli ise geri dönüş, döngü ve rastgele geçişleri doğal olarak destekler:
> - Çözüldü → Yeniden Açıldı (geri dönüş)
> - Onaylandı → Değişiklik İstendi → Draft (geri döngü)
> - Test Fail → Geliştirme → Test (loop)
> - Herhangi durum → İptal (rastgele geçiş)
> - Paralel onay (fork/join)
>
> **B Planı:** Elsa'da beklenmeyen bir sınırlama çıkarsa, MassTransit Saga (state machine) alternatif olarak kullanılabilir. Ancak Elsa'nın görsel designer avantajı (admin panelden akış tasarlama) tercih sebebidir.

**Örnek akışlar (tümü Elsa'da tanımlı):**

| Departman | Kategori | Elsa Akışı |
|-----------|----------|-----------|
| IT | Uygulama Destek | Bekliyor → İşleme Alındı → Çözüldü → Kapandı |
| IT | Yeni Feature | Talep → Analiz → Geliştirme → Test → Deployment → Kapandı |
| IT | Büyük Proje Talebi | Talep → Ön Değerlendirme → Onay → **Projeye Dönüşür** |
| Üye Dept. | Üyelik Başvurusu | Başvuru → Doküman Kontrol → Onay → Aktivasyon |
| Üye Dept. | Harcama İtirazı | İtiraz → İnceleme → Karar (Kabul/Red) → Bildirim |
| Finans | Fatura Onayı | Giriş → Kontrol → Çoklu Onay → Ödeme Planla |
| İK | İzin Talebi | Talep → Yönetici Onay → İK Onay → Onaylandı |

**Elsa akışında kullanılabilecek aktiviteler:**
- **SetStatus** → Ticket durumunu güncelle
- **AssignTo** → Kişi/rol/departmana ata
- **SendNotification** → Bildirim gönder (email, in-app, push)
- **ApprovalGate** → Onay bekle (tek/çoklu onay)
- **Timer** → SLA timeout, otomatik eskalasyon
- **CreateProject** → Projeye dönüştür (efor eşiği aşıldığında)
- **RunAutomation** → Automation Rules tetikle
- **Condition** → Koşullu dallanma (öncelik, tip, departman, alan değeri)

### Projeye Dönüşme

```
Ticket oluşturuldu
  → Elsa akışı içinde:
    IF efor > RequestCategory.AutoProjectThreshold
    OR kategori = "Proje Talebi"
      → Proje onay adımına dal
      → Onay → Project entity oluştur + backlog'a geçir
      → Ticket ↔ Project link kalır (traceability)
```

### Integration Events

| Event | Tetikleyen | Dinleyen |
|-------|-----------|----------|
| `TicketCreatedEvent` | Yeni ticket | Notification, SLA (sayaç başlat), Elsa (akış başlat) |
| `TicketAssignedEvent` | Atama yapıldı | Notification |
| `TicketSlaBreachedEvent` | SLA süresi aşıldı | Notification (eskalasyon), Reporting |
| `TicketConvertedToProjectEvent` | Proje'ye dönüştürüldü | ProjectManagement |

### Talep Sahibi Portalı

Basit, self-service portal:
- Departman + kategori seç → dinamik form açılır (RequestCategory.FormSchema)
- Taleplerimin durumu (Elsa'dan gelen güncel adım gösterilir)
- Yorum ekleme, dosya yükleme
- Bildirimler (e-posta + in-app + push)

---

## Modül 2: Project & Portfolio Management

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **Program** | Name, Description, Owner, Status, StartDate, EndDate | → Projects (1:N) |
| **Project** | Name, Code, Description, Status, StartDate, EndDate, Budget, ProjectManager, **Methodology** (Scrum/Kanban/ScrumBan) | → Program (N:1), → BacklogItems (1:N), → Tickets (talep bağlantısı) |
| **BacklogItem** | Number, Title, Description, Type (UserStory/Task/Bug/Epic/TechDebt), Priority, Status, StoryPoints, EstimatedHours, ActualHours | → Project, → Sprint (Scrum), → Assignee, → Release, → Requirement (opsiyonel), → ParentItem (Epic→Story hiyerarşi) |
| **Sprint** | Name, Goal, StartDate, EndDate, Status (Planning/Active/Completed), Capacity | → Project (sadece Scrum/ScrumBan), → BacklogItems (N:N) |
| **SprintRetrospective** | WentWell, NeedsImprovement, ActionItems | → Sprint |
| **BoardColumn** | Name, Order, WipLimit (null=limitsiz), IsDone (son kolon mu) | → Project |
| **TeamMember** | Role (Dev/QA/BA/PM), Capacity (saat/gün), AvailableFrom, AvailableTo | → Project, → User |

### Çalışma Metodolojisi

Her proje oluşturulurken metodoloji seçilir:

| | Scrum | Kanban | ScrumBan |
|---|---|---|---|
| Sprint | ✅ Var | ❌ Yok | ✅ Var (esnek) |
| Sprint Planlama | ✅ Zorunlu | ❌ Yok | ⚠️ Opsiyonel |
| Board WIP Limit | ⚠️ Opsiyonel | 🔴 Zorunlu | 🔴 Zorunlu |
| Backlog refinement | ✅ Sprint öncesi | ✅ Sürekli | ✅ Sürekli |
| Teslimat | Sprint sonunda | Sürekli (item bittiğinde) | Hibrit |
| Temel metrik | Velocity (SP/sprint) | Throughput + Cycle Time | Her ikisi |

### Backlog Item Durumları

```
UserStory/Task:  New → Refined → Ready → InSprint → InProgress → InReview → Done
Bug:             New → Confirmed → InProgress → Fixed → Verified → Closed
Epic:            New → InProgress → Done (alt item'lar tamamlanınca)
```

### Backlog Hiyerarşisi

```
Epic (büyük iş parcası)
├── User Story (kullanıcı hikayesi)
│   ├── Task (geliştirme görevi)
│   ├── Task
│   └── Bug (test'ten dönen)
└── User Story
    ├── Task
    └── Task
```

### Efor Tahmini

| Yöntem | Kullanım |
|--------|----------|
| **Story Points** | Backlog item'lara göreceli büyüklük (Fibonacci: 1,2,3,5,8,13,21) |
| **Saat** | Task seviyesinde detaylı tahmin |
| **T-Shirt** | Henüz netleşmemiş item'lar (XS, S, M, L, XL → otomatik SP dönüşümü) |
| **AI Tahmini** | Geçmiş tamamlanmış item'lar → yeni item'a otomatik tahmin önerisi |

### Board (Scrum & Kanban ortak)

```
┌──────────┬──────────┬──────────┬──────────┬──────────┐
│   New    │  Active  │ In Review│  Testing │   Done   │
│          │ WIP: 4   │ WIP: 3   │ WIP: 3   │          │
├──────────┼──────────┼──────────┼──────────┼──────────┤
│ [Card]   │ [Card]   │ [Card]   │          │ [Card]   │
│ [Card]   │          │          │ [Card]   │ [Card]   │
│          │ [Card]   │          │          │ [Card]   │
└──────────┴──────────┴──────────┴──────────┴──────────┘
  Drag & drop, filtre (assignee, type)
  Scrum: sprint filtresi aktif, velocity gösterilir
  Kanban: WIP limit aşılırsa kolon kırmızıya döner
  Kolonlar: BoardColumn entity'den gelir (proje bazlı özelleştirilebilir)
```

---

## Modül 3: Requirements & Analysis (Gereksinim ve İş Analizi)

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **Requirement** | Number, Title, Description, Type (Functional/NonFunctional), Priority, Status, AcceptanceCriteria | → Project, → Ticket (kaynak talep), → BacklogItems (1:N), → Approvals |
| **BusinessRule** | RuleNumber, Name, Description, Condition, Action, Status, EffectiveDate | → Project, → Requirement (opsiyonel), → Approvals |
| **Mockup** | Title, Description, ScreenName, Version, FileUrl, ExternalToolLink (Figma/Miro URL) | → Requirement (opsiyonel), → Project |
| **MockupVersion** | VersionNumber, FileUrl, Notes, CreatedAt | → Mockup |
| **AnalysisDocument** | Title, Type (ProcessFlow/DataModel/UseCase/Other), Content (rich text), Status | → Project, → Requirement (opsiyonel) |
| **RequirementApproval** | ApproverRole, Status (Pending/Approved/Rejected), Comment, DecisionDate | → Requirement veya BusinessRule |

### Gereksinim Durumları

```
Draft → InReview → Approved → Implemented → Verified
                 → ChangeRequested → Draft
                 → Rejected
```

### Onay Akışı (Workflow modülü ile)

```
Gereksinim yazıldı
  → İş Analisti onayı
    → Proje Yöneticisi onayı
      → Teknik Lider onayı (NFR ise)
        → Approved → Backlog Item'lara dönüşür
```

### Traceability Matrix

Her gereksinim için:
```
REQ-001 "Kullanıcı login olabilmeli"
  ├── İş Kuralı: BR-003 "3 hatalı denemede hesap kilitlenir"
  ├── Mockup: M-005 "Login Ekranı v2"
  ├── Backlog: US-012 "Login sayfası geliştirme"
  │            US-013 "Hesap kilitleme mekanizması"
  ├── Test: TS-027 "Başarılı login testi"
  │         TS-028 "Hatalı login kilitleme testi"
  └── Release: R-1.2.0
```

---

## Modül 4: Test Management

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **TestScenario** | Number, Title, Preconditions, Steps (ordered), ExpectedResult, Priority, Type (Functional/Regression/Smoke/Performance) | → Project, → Requirement (opsiyonel), → BacklogItem (opsiyonel) |
| **TestStep** | StepOrder, Action, ExpectedResult | → TestScenario |
| **TestPlan** | Name, Description, PlannedDate, Environment, Status | → Project, → Sprint/Release |
| **TestPlanScenario** | Order, AssignedTester | → TestPlan, → TestScenario (N:N) |
| **TestExecution** | Status (NotRun/Pass/Fail/Blocked/Skipped), ActualResult, ExecutedBy, ExecutedAt, Notes, Attachments | → TestPlanScenario |
| **Bug** | → BacklogItem olarak oluşturulur, Type=Bug, TestExecution ile bağlantılı | → TestExecution, → BacklogItem |

### Test Akışı

```
Gereksinim Approved
  → Test senaryoları yazılır (gereksinime bağlı)
    → Test planı oluşturulur (senaryolar seçilir)
      → Sprint/release sonunda test execution başlar
        → Her senaryo: Pass ✅ / Fail ❌ / Blocked ⚠️
          → Fail → Bug oluştur → Backlog'a düşer → re-test
```

### Test Coverage Raporu

```
Gereksinim REQ-001:  3 senaryo → 3 run → 2 pass, 1 fail  → %67 pass rate
Sprint 5:           45 senaryo → 40 pass, 3 fail, 2 blocked → %89 pass rate
Release 1.2:        120 senaryo → coverage: %94
```

---

## Modül 5: Release Management

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **Release** | Version, Name, PlannedDate, ActualDate, Status, ReleaseType (Major/Minor/Patch/Hotfix), Description | → Project |
| **ReleaseItem** | Status (Included/Excluded/Deferred) | → Release, → BacklogItem |
| **GoNoGoChecklist** | — | → Release |
| **GoNoGoItem** | Title, Category (Dev/QA/Ops/Security), Status (Ready/NotReady/NA), Assignee, Notes | → GoNoGoChecklist |
| **ReleaseNote** | Content (auto-generated + editable), IsPublished | → Release |

### Release Akışı

```
Planning → Development → Code Freeze → Testing → Go/No-Go → Deployed → Released
```

### Go/No-Go Kontrol Listesi

```
┌──────────┬───────────────────────────────┬──────────┐
│ Kategori │ Kontrol                       │ Durum    │
├──────────┼───────────────────────────────┼──────────┤
│ Dev      │ Tüm backlog item'lar Done     │ ✅ Ready │
│ QA       │ Test pass rate > %95          │ ✅ Ready │
│ QA       │ Kritik bug yok               │ ⚠️ 1 var │
│ Ops      │ Deployment script hazır       │ ✅ Ready │
│ Security │ Güvenlik taraması geçti       │ ✅ Ready │
│ PM       │ Release note hazır            │ ✅ Ready │
├──────────┼───────────────────────────────┼──────────┤
│          │ KARAR                         │ GO ✅    │
└──────────┴───────────────────────────────┴──────────┘
```

### Release Note Otomatik Üretimi

Backlog item'lardan otomatik:
```
## Release 1.2.0 — 2026-04-15

### Yeni Özellikler
- US-012: Kullanıcı login sayfası yenilendi
- US-015: Dashboard'a yeni widget eklendi

### İyileştirmeler
- US-018: Liste performansı %40 artırıldı

### Bug Düzeltmeleri
- BUG-033: Tarih filtresi yanlış sonuç veriyordu
- BUG-041: PDF export'ta Türkçe karakter sorunu
```

---

## Modül 6: Scheduling Engine (Otomatik Takvim)

### Girdiler

| Girdi | Kaynak |
|-------|--------|
| Backlog item'lar (efor tahmini, öncelik, bağımlılık) | ProjectManagement modülü |
| Ekip kapasitesi (kim, ne zaman, kaç saat/gün) | TeamMember entity |
| Bağımlılıklar (A bitmeden B başlayamaz) | BacklogItem.DependsOn |
| Sprint süreleri (Scrum: 2 hafta döngü) | Sprint entity |
| Throughput (Kanban: ortalama item/hafta) | Geçmiş tamamlanma verisinden hesap |
| Tatiller ve izinler | Takvim/HR entegrasyonu |

**Metodolojiye göre scheduling:**

| | Scrum | Kanban |
|---|---|---|
| Birim | Sprint (2 hafta blok) | Sürekli akış (günlük) |
| Kapasite | Sprint kapasitesi (SP) | Haftalık throughput (item) |
| Tahmin | "Bu item Sprint 5'te" | "Bu item ~3 hafta sonra tamamlanır" |
| Yeniden hesaplama | Periyodik (günlük/haftalık) veya manuel | Periyodik (günlük/haftalık) veya manuel |

### Algoritma

```
1. Backlog'u öncelik sırasına koy (Priority + WSJF score)
2. Her item için:
   a. Bağımlılıkları kontrol et → en erken başlangıç tarihi belirle
   b. Uygun kapasite bul (ekip üyesi + uygun tarih)
   c. Takvime yerleştir
3. Tetikleme:
   a. Periyodik: günlük veya haftalık (Hangfire job)
   b. Manuel: PM "Yeniden Hesapla" butonuna tıklar
   c. Önceki hesaplama ile karşılaştır → kayma varsa bildir
```

### WSJF (Weighted Shortest Job First) Skoru

```
WSJF = (Business Value + Time Criticality + Risk Reduction) / Story Points

Yüksek WSJF → önce yapılmalı
```

### Takvim Görünümü

```
Mart 2026                    Nisan 2026
W1     W2     W3     W4      W1     W2     W3     W4
├──────┤                     
│US-12 │ (Ali, 5 SP)
       ├──────┤
       │US-15 │ (Ayşe, 3 SP)
├─────────────┤
│US-18        │ (Ali, 8 SP)  ← bağımlılık: US-12 sonrası
                              ├──────┤
                              │US-20 │ (Ayşe, 5 SP)
```

### Re-scheduling Tetikleme

| Yöntem | Açıklama |
|--------|----------|
| **Periyodik** | Hangfire job: günlük veya haftalık (PM tarafından konfigüre edilir) |
| **Manuel** | PM proje sayfasında "Takvimi Yeniden Hesapla" butonuna tıklar |

> [!NOTE]
> Her backlog değişikliğinde otomatik tetiklenmez. Bu, gereksiz bildirim gürültüsünü ve performans yükünü önler.

**Bildirim örneği:**
```
"Haftalık takvim güncellemesi:
 Etkilenen item'lar:
 - US-20: 1 hafta ertelendi (Nisan W2 → W3)
 - US-22: 1 hafta ertelendi (Nisan W3 → W4)
 Talep sahipleri bilgilendirildi."
```

---

## Modül 7: Reporting & Analytics

### Dashboard'lar

| Dashboard | İçerik |
|-----------|--------|
| **Yönetici** | program/proje durumu, toplam açık talep, SLA uyum oranı, upcoming releases |
| **Proje Yöneticisi** | sprint burndown, velocity trend, backlog aging, risk, kaynak kullanımı |
| **İş Analisti** | gereksinim durumu dağılımı, onay bekleyen, coverage |
| **QA** | test pass rate, bug trend, açık bug severity dağılımı, regression status |
| **Talep Sahibi** | taleplerimin durumu, tahmini teslim, SLA bilgisi |

### SLA Raporları

| Metrik | Hesaplama |
|--------|-----------|
| İlk yanıt süresi | Ticket oluşturulma → ilk yorum |
| Çözüm süresi | Ticket oluşturulma → resolved |
| SLA uyum oranı | Süresinde çözülen / toplam |
| Eskalasyon oranı | Eskalasyon edilen / toplam |

### Scrum Metrikleri

| Metrik | Açıklama |
|--------|----------|
| Velocity | Sprint başına tamamlanan SP (trend) |
| Burndown | Sprint içi kalan iş (günlük) |
| Sprint hedef tutturma | Planlanan vs tamamlanan SP |
| Bug injection rate | Sprint'te bulunan bug / toplam item |

### Kanban Metrikleri

| Metrik | Açıklama |
|--------|----------|
| Lead Time | Talep oluşturulma → tamamlanma süresi |
| Cycle Time | İşe başlama (Active) → tamamlanma süresi |
| Throughput | Hafta/ay başına tamamlanan item sayısı |
| WIP Yaşlanma | Bir item kaç gündür aynı kolonda duruyor |
| Cumulative Flow | Kolonlardaki item dağılımının zaman grafiği (darboğaz tespiti) |

---

## Modül 8: Developer Tools & IDE Entegrasyonu

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **CommitLink** | CommitHash, Message, Author, Timestamp, Branch, RepositoryUrl | → BacklogItem (N:1) |
| **Repository** | Name, Url, Provider (GitHub/AzureDevOps/GitLab), WebhookSecret | → Project |

### Git Webhook Entegrasyonu

```
Git push → Webhook → Platform API
  1. Commit mesajından [WI-XXX] parse et
  2. BacklogItem'a commit bilgisini bağla
  3. Opsiyonel: ilk commit → item durumunu InProgress'e çek
```

### Git Hook (commit-msg)

```bash
# Commit mesajında [WI-XXX] formatı zorunlu
if ! grep -qE '\[WI-[0-9]+\]' "$1"; then
  echo "❌ Format: [WI-123] mesaj"
  exit 1
fi
```

### Branch Naming Convention

```
feature/WI-456-login-page
bugfix/WI-789-date-filter
hotfix/WI-012-security-patch
```

### VS Code Extension

| Özellik | Açıklama |
|---------|----------|
| **Task Panel** | Sidebar'da bana atanmış backlog item listesi |
| **Active Task** | Status bar'da aktif item (WI-456) |
| **Auto Prefix** | Commit mesajına otomatik `[WI-456]` ekler |
| **Durum Güncelleme** | Extension içinden item durumunu değiştir |
| **Branch Oluşturma** | Item'dan otomatik branch: `feature/WI-456-login-page` |
| **Süre Kayıt** | Çalışma süresini item'a kaydet (start/stop timer) |

### CLI Aracı

```bash
wi list --assigned-to me --status InProgress
wi start WI-456           # branch oluştur + durum güncelle
wi commit "mesaj"          # → git commit -m "[WI-456] mesaj"
wi review WI-456           # durumu InReview'a çek
wi log WI-456 2h           # 2 saat süre kaydet
```

### Frontend

- BacklogItem detayında "Bağlı Commitler" sekmesi (hash, mesaj, author, tarih)
- Commit'ten branch ve diff linkine tıklama (repository URL)

---

## Modül 9: Knowledge Base / Wiki

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **WikiSpace** | Name, Description, Icon | → Project (opsiyonel, proje bağımsız da olabilir) |
| **WikiPage** | Title, Content (rich text/Markdown), Slug, Order, IsPublished | → WikiSpace, → ParentPage (hiyerarşi), → Author |
| **WikiPageVersion** | VersionNumber, Content, EditedBy, EditedAt, ChangeNote | → WikiPage |

### Kullanım Alanları

- Proje teknik dokümantasyonu (mimari kararlar, API rehberi)
- Süreç rehberleri (deployment adımları, onboarding)
- Paylaşılan bilgi bankası (kurumsal standartlar, kod convention)
- Gereksinim ve analiz dokümanlarından farklı: wiki bağımsız, serbest yapıda

### Özellikler

- Sayfa hiyerarşisi (tree navigation)
- Rich text editör (Markdown + WYSIWYG)
- Versiyon geçmişi ve diff görünümü
- Arama (full-text + AI semantic search entegrasyonu)
- Sayfa şablonları (RFD template, meeting notes, ADR)

---

## Modül 10: Change Request (Değişiklik Talebi)

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **ChangeRequest** | Number, Title, Reason, ImpactAnalysis, Status, RequestedBy, RequestedAt | → Requirement veya BacklogItem (değişen kayıt) |
| **ChangeRequestApproval** | ApproverRole, Decision, Comment, DecisionDate | → ChangeRequest |

### Akış

```
Gereksinim/BacklogItem onaylandıktan sonra değişiklik istendi
  → ChangeRequest oluştur (neden, etki analizi)
    → Onay akışı (BA → PM → Teknik Lider)
      → Approved → Gereksinim güncellenir + yeni versiyon
                  → Etkilenen backlog item'lar işaretlenir
                  → Scheduling Engine yeniden hesaplar
      → Rejected → kayıt altında kalır
```

### Traceability

```
REQ-001 v1 → Approved → CR-005 (alan ekleme talebi) → Approved → REQ-001 v2
  Etkilenen: US-012, TS-027 → yeniden değerlendirme gerekir
```

---

## Modül 11: Time Tracking (Süre Kaydı)

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **TimeEntry** | Date, Hours, Description, EntryType (Manual/Timer) | → BacklogItem, → User |

### Kullanım

```bash
# CLI ile
wi log WI-456 3h "Login sayfası frontend"

# VS Code extension ile
Start Timer → çalış → Stop Timer → otomatik kayıt

# Web UI ile
BacklogItem detay → "Süre Ekle" butonu → saat + açıklama
```

### Raporlama

| Rapor | İçerik |
|-------|--------|
| Kişi bazlı zaman çizelgesi | Günlük/haftalık saat dağılımı |
| Proje bazlı harcanan süre | Tahmini vs gerçekleşen |
| Backlog item doğruluğu | EstimatedHours vs ActualHours sapma oranı |

---

## Modül 12: Risk Yönetimi

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **Risk** | Title, Description, Probability (1-5), Impact (1-5), RiskScore (auto: P×I), Status (Open/Mitigated/Closed), MitigationPlan, Owner | → Project |

### Risk Matrisi

```
            Etki
          1   2   3   4   5
       ┌───┬───┬───┬───┬───┐
    5  │ 5 │10 │15 │20 │25 │  ← Kritik (≥15)
    4  │ 4 │ 8 │12 │16 │20 │  ← Yüksek (10-14)
O   3  │ 3 │ 6 │ 9 │12 │15 │  ← Orta (5-9)
l   2  │ 2 │ 4 │ 6 │ 8 │10 │  ← Düşük (1-4)
.   1  │ 1 │ 2 │ 3 │ 4 │ 5 │
       └───┴───┴───┴───┴───┘
```

### Frontend

- Proje detayında "Riskler" sekmesi
- Risk matrisi görsel (ısı haritası)
- PM dashboard'da açık risk sayısı ve en kritik 3 risk

---

## Modül 13: Automation Rules (Otomasyon Kuralları)

### Entity'ler

| Entity | Alanlar | İlişki |
|--------|---------|--------|
| **AutomationRule** | Name, Trigger, Condition, Action, IsActive | → Project (opsiyonel, global da olabilir) |

### Trigger → Condition → Action

```
WHEN  [Trigger]          IF [Condition]              THEN [Action]
──────────────────────────────────────────────────────────────────
BacklogItem.StatusChanged  Type=UserStory              Tüm child Task'lar Done ise
                           AND AllChildrenDone          → Story'yi Done yap

Ticket.Created            Priority=Critical            → PM'e bildirim gönder
                                                       → SLA sayacını 2x hızlandır

Sprint.Completed          UnfinishedItems > 0          → Kalan item'ları sonraki sprint'e taşı

Bug.Created               Severity=Blocker             → Release status'unu Code Freeze'e çek
                                                       → QA Lead'e bildir

TimeEntry.Created         Item.ActualHours >            → PM'e uyarı: "Tahmin aşıldı"
                          Item.EstimatedHours × 1.2
```

### Frontend

- Kural tanımlama wizard: Trigger seç → Condition yaz → Action seç
- Aktif/pasif toggle
- Çalışma geçmişi (hangi kural ne zaman tetiklendi)

---

## Cross-Cutting: Ek Özellikler

### Cross-Project Dependency (Projeler Arası Bağımlılık)

```
Proje A / US-12  ──depends on──►  Proje B / US-45
```

- BacklogItem.DependsOn mevcut proje içi çalışıyor → projeler arası da desteklenmeli
- Scheduling Engine: farklı projelerdeki bağımlılıkları da hesaplar
- PM dashboard: cross-project dependency haritası

### PWA (Progressive Web App)

- next.config.js: PWA manifest, service worker
- Mobil onay: bildirim gelir → aç → onayla/reddet
- Offline: son görüntülenen item'lar cache'te
- Home screen'e ekle (install prompt)

---

## Modüller Arası Event Haritası

```
RequestMgmt                ProjectMgmt                 Requirements
  ITRequestApproved ──────► CreateProjectOrLinkBacklog
  TicketSlaBreached ──────► (Notification)
                            BacklogItemCompleted ──────► (Notification → talep sahibi)
                            SprintCompleted ───────────► TestMgmt: TestPlanReady
                                                         ReleaseMgmt: ItemsReady

Requirements               ChangeRequest               TestMgmt
  RequirementApproved ────► BacklogItemsCreated          TestFailed ──► BugCreated → BacklogItem
  BusinessRuleApproved ──► (Notification)                AllTestsPassed ► ReleaseMgmt: QAReady
                            CRApproved ────────────────► RequirementUpdated
                                                         → BacklogItemsReEvaluate
                                                         → SchedulingEngine: Reschedule

ReleaseMgmt                AutomationRules              SchedulingEngine
  ReleaseDeployed ────────► (Notification → herkes)      BacklogPriorityChanged ► Reschedule
                            Any Event ─────────────────► Rule Evaluate → Action
                            NewItemAdded ──────────────► Reschedule → Notification
                            CrossProjectDepChanged ────► Reschedule (multi-project)
```


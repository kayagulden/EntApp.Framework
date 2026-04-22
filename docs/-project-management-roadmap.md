# 16b - Project & Portfolio Management - Detayli Yol Haritasi

> **Ana roadmap:** [-roadmap.md (Faz 16)](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-roadmap.md#faz-16--delivery-platform-almitsm-modulleri)
> **Backend entity evrim:** 2026-04-21 (Unified Work Item Model)
> **Ilgili plan:** [unified-workitem-plan.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/unified-workitem-plan.md)

> [!IMPORTANT]
> **Terminoloji Degisikligi:** Eski `TaskItemBase`/`BacklogItem` kavramlari `WorkItemBase` olarak birlestirildi.
> `TaskNumber` -> `WorkItemNumber`, `TaskId` -> `WorkItemId`, API route `/api/pm/tasks` -> `/api/pm/work-items`.
> Hiyerarsi artik `WorkItemType` enum uzerinden (Epic, Feature, UserStory, Task, Bug, TechDebt, Spike).

---

## Tamamlanan Maddeler

### Portfolio & Proje Altyapisi
- [x] `PortfolioBase` entity (projeleri stratejik gruplama)
- [x] `ProjectBase` genisletme: PortfolioId, OwnerUserId, TargetEndDate, Methodology, Category
- [x] Metodoloji destegi: Scrum / Kanban / ScrumBan / Waterfall
- [x] Proje kategorileri: SoftwareDevelopment / Infrastructure / Procurement / Business / General
- [x] Portfolio & Proje CRUD API + Frontend sayfalari

### WorkItemBase Evolution (eski BacklogItem)
- [x] `WorkItemBase` entity: StoryPoints, AcceptanceCriteria, SprintId, HierarchyLevel alanlari
- [x] `WorkItemType` enum: Task, Bug, Feature, Improvement, Epic, UserStory, TechDebt, Spike
- [x] `WorkItemStatus` enum: Backlog, Todo, InProgress, InReview, Done, Cancelled
- [x] `WorkItemHierarchyRules` - tip bazli ebeveyn-cocuk kurallari (Epic->Feature->Story->Task)
- [x] `WorkItemNumberGenerator` - proje bazli (KEY-N) ve bagimsiz (WI-N) numaralama
- [x] WorkItem CRUD + Update + AssignToSprint API (tum endpoint'ler `/api/pm/work-items`)

### Sprint Yonetimi
- [x] `SprintBase` entity (Planning -> Active -> Completed lifecycle)
- [x] PlannedPoints, CompletedPoints, CapacityPoints alanlari
- [x] Sprint CRUD API: 7 endpoint (`/api/pm/projects/{id}/sprints`, `/api/pm/sprints/{id}`)
- [x] Sprint baslatma / tamamlama (Start/Complete)
- [x] Work item <-> Sprint atama (`AssignToSprintCommand`)

### Board & Kanban
- [x] `BoardColumn` entity (projeye bagli, ozellestirilebilir kolonlar)
- [x] WipLimit - asildiginda UI uyari
- [x] MappedStatus - kolon <-> WorkItemStatus eslemesi
- [x] Proje olusturuldugunda varsayilan 6 kolon otomatik olusturma
- [x] BoardColumn CRUD + Reorder API (5 endpoint)
- [x] Kanban Board API (`/api/pm/work-items/board/{projectId}`)

### Backlog & Hiyerarsi
- [x] Backlog API (`/api/pm/projects/{id}/backlog`) - flat veya tree gorunum
- [x] Filtreleme: type, sprint (current/id), assignee, status

> [!NOTE]
> **Tasarim karari:** Backlog ayri bir tab degil, "Is Kalemleri" tab'i icinde Tablo/Agac gorunum toggle'i ile sunulacak.
> Backlog ayri bir entity degildir - projenin WorkItem koleksiyonunun siralanmis gorunumudur.

### Velocity & Burndown Metrikleri
- [x] `BurndownSnapshot` entity - günlük SP kaydı
- [x] Her MoveWorkItem'da aktif sprint snapshot otomatik upsert
- [x] Velocity API (`GET /api/pm/projects/{id}/velocity`) - sprint bazlı bar chart data
- [x] Burndown API (`GET /api/pm/sprints/{id}/burndown`) - line chart data

### Milestone (Kilometre Taşı)
- [x] `MilestoneBase` entity oluşturuldu (ProjectId, Name, DueDate, Status, Description, SortOrder)
- [x] WorkItemBase & SprintBase'e MilestoneId (nullable FK) eklendi
- [x] Milestone CRUD API: 4 endpoint (`/api/pm/projects/{projectId}/milestones`)
- [x] Milestones tab UI: timeline görünümü, oluşturma/düzenleme/silme formları

---

## Devam Edilecek Maddeler

### İş Kalemleri Tab'ı (Proje Detay Sayfası)
- [x] "İş Kalemleri" tab'ını aktifleştirme → **"Backlog" olarak yeniden adlandırıldı**
- [x] Tablo görünümü: flat liste, filtreleme (tip, status, sprint, assignee)
- [ ] Ağaç görünümü: hiyerarşik (Epic->Feature->Story->Task), drag & drop sıralama
- [x] Proje içinden yeni WorkItem oluşturma formu (tip seçimli)
- [ ] Sprint'e atama (inline veya modal)

### Kanban Board Gelistirmeleri
- [ ] Drag & drop kart surukleme (frontend)
- [ ] Swimlane destegi (assignee, priority, type bazli)
- [ ] Quick filter: "Benim itemlarim", tip bazli, sprint bazli

### Metrik Gorsellestirme
- [ ] Frontend burndown line chart gorsellestirme
- [ ] Kanban metrikleri: Lead Time, Cycle Time, WIP yaslanma

### Efor & Onceliklendirme
- [ ] Efor tahmini: Saat, T-Shirt, AI tahmini (StoryPoints mevcut)
- [ ] WSJF (Weighted Shortest Job First) onceliklendirme

### Cross-Module Bağlantılar
- [x] Ticket -> Proje bağlantısı: **"Projeye Aktar" modalı** (proje/tip/öncelik seçimi, mevcut task'ları taşıma, aktarıldıktan sonra kilitli durum)

### Proje Template Sistemi
- [ ] Kategori bazli varsayilan ayarlar: hangi sekmeler/ozellikler aktif, hangi WorkItemType'lar gecerli, varsayilan BoardColumn'lar
- [ ] UI label eslemeleri (Epic->Asama, Story->Is Paketi)
- [ ] Metodoloji kisitlamalari
- [ ] Template DB veya JSON tabanlı, runtime konfigürabl

### State Flow Engine Entegrasyonu
> **Detaylar:** [-state-flow-engine-roadmap.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-state-flow-engine-roadmap.md)

- [ ] WorkItem state geçişlerini `StateFlowEngine` üzerinden yönetme (Elsa 3 yerine)
- [ ] Proje bazlı özel state akışı tanımlama (varsayılan: Backlog → Todo → InProgress → InReview → Done)
- [ ] Board/Kanban sürükleme → StateFlowEngine validasyonu
- [ ] React Flow Designer'da WorkItem akışı tasarlama
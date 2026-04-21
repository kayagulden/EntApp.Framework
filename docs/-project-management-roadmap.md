# 16b — Project & Portfolio Management — Detaylı Yol Haritası

> **Ana roadmap:** [-roadmap.md (Faz 16)](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-roadmap.md#faz-16--delivery-platform-almitsm-mod�lleri)
> **Backend entity evrim:** 2026-04-21 (Unified Work Item Model)
> **İlgili plan:** [unified-workitem-plan.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/unified-workitem-plan.md)

> [!IMPORTANT]
> **Terminoloji Değişikliği:** Eski `TaskItemBase`/`BacklogItem` kavramları `WorkItemBase` olarak birleştirildi.
> `TaskNumber` → `WorkItemNumber`, `TaskId` → `WorkItemId`, API route `/api/pm/tasks` → `/api/pm/work-items`.
> Hiyerarşi artık `WorkItemType` enum üzerinden (Epic, Feature, UserStory, Task, Bug, TechDebt, Spike).

---

## ✅ Tamamlanan Maddeler

### Portfolio & Proje Altyapısı
- [x] `PortfolioBase` entity (projeleri stratejik gruplandırma)
- [x] `ProjectBase` genişletme: PortfolioId, OwnerUserId, TargetEndDate, Methodology, Category
- [x] Metodoloji desteği: Scrum / Kanban / ScrumBan / Waterfall
- [x] Proje kategorileri: SoftwareDevelopment / Infrastructure / Procurement / Business / General
- [x] Portfolio & Proje CRUD API + Frontend sayfaları

### WorkItemBase Evolution (eski BacklogItem)
- [x] `WorkItemBase` entity: StoryPoints, AcceptanceCriteria, SprintId, HierarchyLevel alanları
- [x] `WorkItemType` enum: Task, Bug, Feature, Improvement, Epic, UserStory, TechDebt, Spike
- [x] `WorkItemStatus` enum: Backlog, Todo, InProgress, InReview, Done, Cancelled
- [x] `WorkItemHierarchyRules` — tip bazlı ebeveyn-çocuk kuralları (Epic→Feature→Story→Task)
- [x] `WorkItemNumberGenerator` — proje bazlı (KEY-N) ve bağımsız (WI-N) numaralama
- [x] WorkItem CRUD + Update + AssignToSprint API (tüm endpoint'ler `/api/pm/work-items`)

### Sprint Yönetimi
- [x] `SprintBase` entity (Planning → Active → Completed lifecycle)
- [x] PlannedPoints, CompletedPoints, CapacityPoints alanları
- [x] Sprint CRUD API: 7 endpoint (`/api/pm/projects/{id}/sprints`, `/api/pm/sprints/{id}`)
- [x] Sprint başlatma / tamamlama (Start/Complete)
- [x] Work item ↔ Sprint atama (`AssignToSprintCommand`)

### Board & Kanban
- [x] `BoardColumn` entity (projeye bağlı, özelleştirilebilir kolonlar)
- [x] WipLimit — aşıldığında UI uyarı
- [x] MappedStatus — kolon ↔ WorkItemStatus eşlemesi
- [x] Proje oluşturulduğunda varsayılan 6 kolon otomatik oluşturma
- [x] BoardColumn CRUD + Reorder API (5 endpoint)
- [x] Kanban Board API (`/api/pm/work-items/board/{projectId}`)

### Backlog & Hiyerarşi
- [x] Backlog API (`/api/pm/projects/{id}/backlog`) — flat veya tree görünüm
- [x] Filtreleme: type, sprint (current/id), assignee, status

### Velocity & Burndown Metrikleri
- [x] `BurndownSnapshot` entity — günlük SP kaydı
- [x] Her MoveWorkItem'da aktif sprint snapshot otomatik upsert
- [x] Velocity API (`GET /api/pm/projects/{id}/velocity`) — sprint bazlı bar chart data
- [x] Burndown API (`GET /api/pm/sprints/{id}/burndown`) — line chart data

---

## 📋 Devam Edilecek Maddeler

### Kanban Board Geliştirmeleri
- [ ] Drag & drop kart sürükleme (frontend)
- [ ] Swimlane desteği (assignee, priority, type bazlı)
- [ ] Quick filter: "Benim itemlarım", tip bazlı, sprint bazlı

### Metrik Görselleştirme
- [ ] Frontend burndown line chart görselleştirme
- [ ] Kanban metrikleri: Lead Time, Cycle Time, WIP yaşlanma

### Efor & Önceliklendirme
- [ ] Efor tahmini: Saat, T-Shirt, AI tahmini (StoryPoints mevcut)
- [ ] WSJF (Weighted Shortest Job First) önceliklendirme

### Cross-Module Bağlantılar
- [ ] Ticket → Proje bağlantısı (ConvertToProject, AddToBacklog)

### Proje Template Sistemi
- [ ] Kategori bazlı varsayılan ayarlar: hangi sekmeler/özellikler aktif, hangi WorkItemType'lar geçerli, varsayılan BoardColumn'lar
- [ ] UI label eşlemeleri (Epic→Aşama, Story→İş Paketi)
- [ ] Metodoloji kısıtlamaları
- [ ] Template DB veya JSON tabanlı, runtime konfigürabl

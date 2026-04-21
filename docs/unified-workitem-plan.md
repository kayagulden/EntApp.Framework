# Unified Work Item Model — İmplementasyon Planı

> **Tarih:** 2026-04-21
> **Modül:** TaskManagement (`pm` schema) + RequestManagement (`req` schema)
> **Ön koşul:** Mevcut WorkItemBase, Ticket, ProjectBase entity'leri çalışır durumda
> **İlgili Roadmap:** [delivery-platform-roadmap.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/delivery-platform-roadmap.md)

---

## Motivasyon

Mevcut sistemde iki ayrı "iş yapma" kavramı var:

1. **Ticket (Talep)** — dış dünyadan gelen istekler, SLA/Workflow ile yönetilir
2. **WorkItemBase (İş Kalemi)** — projeye bağlı veya bağımsız iş kalemleri, Kanban/Scrum ile yönetilir

Bu kavramlar örtüşüyor: bir talep aslında bir Epic, Feature veya User Story'dir; talebe bağlı görevler ise zincirin sonundaki Task'tır. İkisini temiz bir modelle birleştirmek gerekiyor.

### Temel İlke

```text
Ticket = INTAKE (ne isteniyor?)  →  ayrı entity, değişmez
WorkItem = EXECUTION (ne yapılacak?)  →  WorkItemBase, hiyerarşi kazanır
```

---

## İsimlendirme Tablosu

> [!IMPORTANT]
> Aşağıdaki isimler Faz 1-3 sonrasında tüm kod tabanında uygulanmıştır.

| Kavram | C# İsmi | DB Tablosu | API Route |
|--------|---------|-----------|-----------|
| İş kalemi entity | `WorkItemBase` | `pm.work_items` | `/api/pm/work-items` |
| İş kalemi ID | `WorkItemId` | — | — |
| İş kalemi tipi | `WorkItemType` enum | — | — |
| İş kalemi durumu | `WorkItemStatus` enum | — | — |
| İş kalemi önceliği | `WorkItemPriority` enum | — | — |
| İş kalemi numarası | `WorkItemNumber` property | `WorkItemNumber` col | — |
| Sprint entity | `SprintBase` | `pm.sprints` | `/api/pm/sprints` |
| Sprint ID | `SprintId` | — | — |
| Sprint durumu | `SprintStatus` enum | — | — |
| Backlog | — | — | `/api/pm/projects/{id}/backlog` |

---

## ✅ Faz 1 — WorkItemBase Evolution (TAMAMLANDI)

- [x] `WorkItemType` enum'ına `UserStory`, `TechDebt`, `Spike` eklendi
- [x] `SprintStatus` enum eklendi
- [x] `SprintId`, `BoardColumnId` strongly-typed ID'ler eklendi
- [x] `WorkItemBase`'e `StoryPoints`, `AcceptanceCriteria`, `SprintId`, `HierarchyLevel` eklendi
- [x] Setter metodları ve `Update()` metodu güncellendi

---

## ✅ Faz 2 — Sprint Entity & API (TAMAMLANDI)

- [x] `SprintBase` entity oluşturuldu (Planning → Active → Completed lifecycle)
- [x] Sprint CRUD API (6 endpoint)
- [x] `AssignToSprintCommand` + endpoint

### Sprint API Endpoints

| Endpoint | Method | Açıklama |
|----------|--------|----------|
| `/api/pm/projects/{projectId}/sprints` | GET | Proje sprintlerini listele |
| `/api/pm/projects/{projectId}/sprints` | POST | Yeni sprint oluştur |
| `/api/pm/sprints/{id}` | GET | Sprint detayı (work items dahil) |
| `/api/pm/sprints/{id}` | PUT | Sprint güncelle |
| `/api/pm/sprints/{id}/start` | POST | Sprint başlat |
| `/api/pm/sprints/{id}/complete` | POST | Sprint tamamla |
| `/api/pm/work-items/{id}/sprint` | POST | Work item'ı sprint'e ata/çıkar |

---

## ✅ Faz 3 — Hiyerarşi Kuralları & Backlog View (TAMAMLANDI)

- [x] `WorkItemHierarchyRules` domain kuralları oluşturuldu
- [x] Backlog API (flat/tree view + filtreler)

### Hiyerarşi Kuralları

```text
Epic       → [Feature, UserStory]
Feature    → [UserStory, Task, Bug]
UserStory  → [Task, Bug]
Task       → [] (leaf)
Bug        → [Task]
TechDebt   → [Task]
Spike      → [Task]
```

### Backlog API

```text
GET /api/pm/projects/{projectId}/backlog
    ?view=flat|tree
    &type=Epic,Feature,...
    &sprint=current|{id}
    &assignee={userId}
    &status=Backlog,Todo,...
```

---

## Faz 4 — Ticket ↔ WorkItem Bağlantı Mekanizması

> **Hedef:** Talep (Ticket) ve Work Item arasındaki ilişkiyi netleştir.

### 4a — Bağlantı Modeli

Mevcut `SourceModule/SourceType/SourceId` mekanizması zaten esnek. Bunu koruyoruz:

```text
Ticket (RequestManagement)  ──SourceId──→  WorkItem (TaskManagement)
  "Şifre sıfırlama istiyorum"              UserStory: "Şifre Sıfırlama"
                                              ├── Task: "API endpoint yaz"
                                              ├── Task: "Email template"
                                              └── Task: "UI sayfası yap"
```

### 4b — Ticket'tan WorkItem Oluşturma Akışları

| Senaryo | Tetikleyici | Oluşan WorkItem |
|---------|------------|-----------------|
| Küçük talep | Elsa workflow veya manuel | Task (mevcut davranış, aynen korunur) |
| Orta talep | Operatör "İş Kalemi Oluştur" der | UserStory + alt Task'lar |
| Büyük talep | PM "Projeye Dönüştür" der | Epic (yeni proje altında) |

### 4c — API Endpoint

```json
POST /api/pm/work-items/from-ticket
{
    "ticketId": "...",
    "workItemType": "UserStory",
    "projectId": "...",
    "title": "...",
    "parentWorkItemId": "..."
}
```

### 4d — Ticket Detay UI Güncelleme

Ticket detay sayfasında mevcut "Bağlı Görevler" bölümü genişler:

```text
Bağlı İş Kalemleri
  ├── 📘 UserStory: Şifre Sıfırlama (US-12)    [InProgress]
  │     ├── ✅ Task: API endpoint (PROJ-45)      [Done]
  │     ├── 🔄 Task: Email template (PROJ-46)    [InProgress]
  │     └── ⬜ Task: UI sayfası (PROJ-47)         [Todo]
  └── + İş Kalemi Ekle  (dropdown: Task | UserStory | Feature | Epic)
```

---

## Faz 5 — Board Geliştirme

> **Hedef:** Kanban board'u work item hiyerarşisine uyumlu hale getir.

### 5a — BoardColumn Entity (YENİ)

```csharp
public sealed class BoardColumn : AuditableEntity<BoardColumnId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }
    public string Name { get; private set; }       // "To Do", "In Progress", ...
    public int Order { get; private set; }          // kolon sırası
    public int? WipLimit { get; private set; }      // max item sayısı
    public WorkItemStatus MappedStatus { get; private set; } // hangi status'a eşlendiği
}
```

### 5b — Board UI İyileştirmeleri

- WIP limit aşımında kolon header kırmızı
- Swimlane desteği (assignee, priority, type bazlı)
- Card üzerinde: tip ikonu, SP badge, assignee avatar, subtask progress bar
- Quick filter: "Benim itemlarım", tip bazlı, sprint bazlı

---

## Faz 6 — Velocity & Burndown Charts

> **Hedef:** Scrum metrikleri.

### 6a — Velocity Chart

- Sprint tamamlandığında SP toplamı kaydedilir
- Sprint bazlı velocity trend grafiği (bar chart)
- Ortalama velocity hesaplama

### 6b — Burndown Chart

- Aktif sprint içinde: kalan SP vs ideal çizgi
- Günlük snapshot (Hangfire job veya sprint board değişikliğinde)

### 6c — Kanban Metrikleri

- Lead Time: oluşturulma → Done
- Cycle Time: InProgress → Done
- WIP yaşlanma: kaç gündür aynı kolonda

---

## Migration Stratejisi

> [!WARNING]
> **Mevcut veri korunmalı.** Tüm değişiklikler geriye dönük uyumlu olmalı.

### DB Migration SQL (Faz 1-3 için)

```sql
-- Tablo rename (eski "tasks" → yeni "work_items")
ALTER TABLE pm.tasks RENAME TO work_items;

-- Yeni kolonlar
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "StoryPoints" integer NULL;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "AcceptanceCriteria" text NULL;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "SprintId" uuid NULL;
ALTER TABLE pm.work_items ADD COLUMN IF NOT EXISTS "HierarchyLevel" integer NOT NULL DEFAULT 0;

-- Kolon rename
ALTER TABLE pm.work_items RENAME COLUMN "TaskNumber" TO "WorkItemNumber";

-- Sprint tablosu
CREATE TABLE IF NOT EXISTS pm.sprints (
    "Id" uuid PRIMARY KEY,
    "ProjectId" uuid NOT NULL REFERENCES pm.projects("Id"),
    "Name" varchar(200) NOT NULL,
    "Goal" varchar(2000),
    "Status" varchar(20) NOT NULL DEFAULT 'Planning',
    "StartDate" timestamp NOT NULL,
    "EndDate" timestamp NOT NULL,
    "CapacityPoints" integer,
    "TenantId" uuid NOT NULL,
    "CreatedAt" timestamp NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp,
    "CreatedBy" varchar(256),
    "ModifiedBy" varchar(256),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "xmin" xid NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_sprints_project ON pm.sprints("ProjectId");
CREATE INDEX IF NOT EXISTS ix_sprints_status ON pm.sprints("Status");

-- SprintId FK
ALTER TABLE pm.work_items ADD CONSTRAINT fk_work_items_sprint 
    FOREIGN KEY ("SprintId") REFERENCES pm.sprints("Id");
CREATE INDEX IF NOT EXISTS ix_work_items_sprint ON pm.work_items("SprintId");

-- Type kolon genişletme (yeni enum değerleri için)
ALTER TABLE pm.work_items ALTER COLUMN "Type" TYPE varchar(30);
```

---

## Dosya Değişiklik Listesi

### Backend (Faz 1-3: TAMAMLANDI)

| Dosya | Değişiklik | Durum |
|-------|-----------|-------|
| `TaskManagementEnums.cs` | WorkItemType'a UserStory, TechDebt, Spike; SprintStatus; WorkItemStatus rename | ✅ |
| `TaskManagementIds.cs` | SprintId, BoardColumnId, WorkItemId rename | ✅ |
| `WorkItemBase.cs` | StoryPoints, AcceptanceCriteria, SprintId, HierarchyLevel + rename | ✅ |
| `SprintBase.cs` | **YENİ** — Sprint entity | ✅ |
| `WorkItemHierarchyRules.cs` | **YENİ** — hiyerarşi doğrulama | ✅ |
| `TaskManagementDbContext.cs` | Sprint, WorkItemBase config + rename | ✅ |
| `TaskManagementQueries.cs` | Sprint/Backlog queries + DTO rename | ✅ |
| `TaskManagementCommands.cs` | Sprint commands + command rename | ✅ |
| `TaskManagementHandlers.cs` | Sprint CRUD handlers + handler rename | ✅ |
| `TaskManagementEndpoints.cs` | Sprint/Backlog endpoints + route rename | ✅ |
| `WorkItemNumberGenerator.cs` | Rename from TaskNumberGenerator | ✅ |
| `TaskManagementEvents.cs` | Integration event rename | ✅ |
| `TicketTaskEventHandler.cs` | Cross-module event handler rename | ✅ |
| `WorkflowTaskCompletionHandler.cs` | Cross-module event handler rename | ✅ |
| `CreateTaskForAssigneeActivity.cs` | Elsa activity command rename | ✅ |

### Backend (Faz 4-6: BEKLEMEDE)

| Dosya | Değişiklik |
|-------|-----------|
| `BoardColumn.cs` | **YENİ** — Board column entity (Faz 5) |
| `TaskManagementEndpoints.cs` | `/api/pm/work-items/from-ticket` endpoint (Faz 4) |
| Velocity/Burndown snapshot entity | **YENİ** (Faz 6) |

### Frontend (Tüm fazlar: BEKLEMEDE)

| Dosya | Değişiklik |
|-------|-----------|
| `work-item-type-badge.tsx` | **YENİ** — tip bazlı ikon + renk badge |
| `backlog-view.tsx` | **YENİ** — flat/tree backlog görünümü |
| `sprint-list.tsx` | **YENİ** — sprint listesi (proje detayında) |
| `sprint-board.tsx` | **YENİ** — sprint filtreli Kanban board |
| `sprint-planning.tsx` | **YENİ** — backlog → sprint drag & drop |
| `estimation-badge.tsx` | **YENİ** — story points badge |
| `velocity-chart.tsx` | **YENİ** — sprint velocity grafiği |
| `burndown-chart.tsx` | **YENİ** — sprint burndown grafiği |
| `ticket detail page` | "İş Kalemi Oluştur" dropdown (Task/Story/Feature/Epic) |
| `project detail page` | Sprint tab, Backlog tab ekleme |

---

## Özet Tablo

| Faz | Başlık | Tahmini Süre | Bağımlılık | Durum |
|-----|--------|-------------|-----------|-------|
| 1 | WorkItemBase Evolution (alan + enum) | 0.5 gün | Yok | ✅ TAMAMLANDI |
| 2 | Sprint Entity & API | 1 gün | Faz 1 | ✅ TAMAMLANDI |
| 3 | Hiyerarşi Kuralları & Backlog View | 1-2 gün | Faz 1 | ✅ TAMAMLANDI |
| — | **Rename: Task → WorkItem** | 0.5 gün | Faz 1-3 | ✅ TAMAMLANDI |
| 4 | Ticket ↔ WorkItem Bağlantı | 1 gün | Faz 1 | ⬜ BEKLEMEDE |
| 5 | Board Geliştirme (WIP, Swimlane) | 1-2 gün | Faz 3 | ⬜ BEKLEMEDE |
| 6 | Velocity & Burndown Charts | 1 gün | Faz 2 | ⬜ BEKLEMEDE |
| | **Toplam** | **~5-8 gün** | | |

> [!NOTE]
> Faz 1-3 + Rename tamamlanmıştır. Devam eden fazlar (4-6) yeni isimlendirmeyi kullanacaktır.
> DB migration henüz uygulanmamıştır — backend başlatılmadan önce yukarıdaki SQL çalıştırılmalıdır.

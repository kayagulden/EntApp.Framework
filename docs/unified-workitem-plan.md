# Unified Work Item Model — İmplementasyon Planı

> **Tarih:** 2026-04-21
> **Modül:** TaskManagement (`pm` schema) + RequestManagement (`req` schema)
> **Ön koşul:** Mevcut TaskItemBase, Ticket, ProjectBase entity'leri çalışır durumda
> **İlgili Roadmap:** [delivery-platform-roadmap.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/delivery-platform-roadmap.md)

---

## Motivasyon

Mevcut sistemde iki ayrı "iş yapma" kavramı var:

1. **Ticket (Talep)** — dış dünyadan gelen istekler, SLA/Workflow ile yönetilir
2. **TaskItemBase (Görev)** — projeye bağlı veya bağımsız görevler, Kanban ile yönetilir

Bu kavramlar örtüşüyor: bir talep aslında bir Epic, Feature veya User Story'dir; talebe bağlı görevler ise zincirin sonundaki Task'tır. İkisini temiz bir modelle birleştirmek gerekiyor.

### Temel İlke

```text
Ticket = INTAKE (ne isteniyor?)  →  ayrı entity, değişmez
WorkItem = EXECUTION (ne yapılacak?)  →  TaskItemBase evolve eder, hiyerarşi kazanır
```

---

## Mevcut Durum Analizi

### TaskItemBase (şu an)

```csharp
// Dosya: TaskManagement.Domain/Entities/TaskItemBase.cs
public sealed class TaskItemBase : AuditableEntity<TaskItemId>, ITenantEntity
{
    ProjectId? ProjectId            // opsiyonel proje bağlantısı
    string TaskNumber               // KEY-1, KEY-2 (proje bazlı) veya TASK-XXX (bağımsız)
    string Title, Description
    TaskStatus Status               // Backlog, Todo, InProgress, InReview, Done, Cancelled
    TaskPriority Priority           // Low, Medium, High, Critical
    TaskType Type                   // Task, Bug, Feature, Improvement, Epic  ← zaten var!
    Guid? AssigneeUserId
    TaskItemId? ParentTaskId        // alt görev desteği ← zaten var!
    string? SourceModule/SourceType/SourceId  // cross-module bağlantı (Ticket)
    int SortOrder                   // Kanban sıralama
    decimal EstimatedHours
}
```

### Güçlü Yanlar (korunacak)

- ✅ `TaskType` enum'ı zaten `Epic, Feature, Task, Bug` içeriyor
- ✅ `ParentTaskId` zaten self-referencing hiyerarşi destekliyor
- ✅ `SourceModule/SourceType/SourceId` ile Ticket bağlantısı zaten polymorfik
- ✅ `ProjectId` nullable — projesiz görevler zaten destekleniyor
- ✅ `SortOrder` — backlog/board sıralaması mevcut

### Eksikler (eklenecek)

- ❌ `StoryPoints` — estimation (Scrum için temel)
- ❌ `AcceptanceCriteria` — User Story kabul kriterleri
- ❌ `SprintId` — sprint bağlantısı
- ❌ `WorkItemType` ayrımı net değil — `TaskType` yeterince granüler değil
- ❌ `UserStory` tipi yok (Feature ≠ UserStory)
- ❌ `TechDebt` tipi yok
- ❌ Hiyerarşi kuralları yok (Epic altında sadece Feature olabilir gibi)
- ❌ `Labels` — tags string yerine entity olmalı
- ❌ `BlockedBy` / `Blocks` — item arası bağımlılık

---

## Faz 1 — TaskItemBase → WorkItem Evolution

> **Hedef:** Mevcut entity'yi bozmadan genişlet. Rename yapmadan alan ekle.

### 1a — Enum Güncellemesi

**Dosya:** `TaskManagement.Domain/Enums/TaskManagementEnums.cs`

```csharp
// TaskType enum güncellenir (mevcut değerler korunur, yeniler eklenir)
public enum TaskType
{
    Task = 0,           // mevcut ✅
    Bug = 1,            // mevcut ✅
    Feature = 2,        // mevcut ✅ — Feature seviyesi work item
    Improvement = 3,    // mevcut ✅
    Epic = 4,           // mevcut ✅ — üst seviye work item
    UserStory = 5,      // YENİ — kullanıcı hikayesi
    TechDebt = 6,       // YENİ — teknik borç
    Spike = 7,          // YENİ — araştırma / PoC
}
```

> [!NOTE]
> `TaskType` ismi yetersiz hale geliyor ama mevcut DB verilerini bozmamak için enum ismi korunabilir.
> İleride `WorkItemType` olarak rename edilebilir (breaking change dikkatli yönetilmeli).

### 1b — TaskItemBase Alan Eklemeleri

**Dosya:** `TaskManagement.Domain/Entities/TaskItemBase.cs`

```csharp
// YENİ ALANLAR (mevcut alanlara ek olarak)

/// <summary>Story Points — Scrum estimation (0, 1, 2, 3, 5, 8, 13, 21).</summary>
public int? StoryPoints { get; private set; }

/// <summary>Kabul kriterleri (Markdown) — UserStory/Feature için.</summary>
public string? AcceptanceCriteria { get; private set; }

/// <summary>Sprint bağlantısı (nullable — sprintsiz çalışma mümkün).</summary>
public SprintId? SprintId { get; private set; }

/// <summary>Hiyerarşi derinliği cache'i — 0:Epic, 1:Feature, 2:Story, 3:Task.</summary>
public int HierarchyLevel { get; private set; }

/// <summary>
/// Kaynak Ticket ID — direkt FK yerine raw Guid (cross-module).
/// Mevcut SourceModule/SourceType/SourceId'nin kısa yolu.
/// </summary>
// NOT: Zaten SourceId ile yapılıyor, ek alan gerekli değil.
```

### 1c — SprintId Eklenmesi

**Dosya:** `TaskManagement.Domain/Ids/TaskManagementIds.cs`

```csharp
public readonly record struct SprintId(Guid Value) : IEntityId;
```

### 1d — DB Migration

```sql
-- Mevcut pm.task_items tablosuna yeni kolonlar
ALTER TABLE pm.task_items ADD COLUMN "StoryPoints" integer NULL;
ALTER TABLE pm.task_items ADD COLUMN "AcceptanceCriteria" text NULL;
ALTER TABLE pm.task_items ADD COLUMN "SprintId" uuid NULL;
ALTER TABLE pm.task_items ADD COLUMN "HierarchyLevel" integer NOT NULL DEFAULT 0;
```

> [!IMPORTANT]
> Mevcut veriler etkilenmez — tüm yeni alanlar nullable veya default değerli.

---

## Faz 2 — Sprint Entity

> **Hedef:** Scrum metodolojisi destekleyen projeler için sprint yönetimi.

### 2a — Sprint Entity

**Dosya:** `TaskManagement.Domain/Entities/SprintBase.cs` (YENİ)

```csharp
public sealed class SprintBase : AuditableEntity<SprintId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }
    public string Name { get; private set; }        // "Sprint 1", "Sprint 2"
    public string? Goal { get; private set; }        // Sprint hedefi
    public SprintStatus Status { get; private set; } // Planning, Active, Completed, Cancelled
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int? CapacityPoints { get; private set; }  // Takım kapasitesi (SP)

    // Navigation
    public ProjectBase Project { get; private set; }
    public ICollection<TaskItemBase> WorkItems { get; private set; } = [];
}
```

### 2b — Sprint Enum

```csharp
public enum SprintStatus
{
    Planning = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}
```

### 2c — Sprint API Endpoints (6 endpoint)

| Endpoint | Method | Açıklama |
|----------|--------|----------|
| `/api/pm/projects/{projectId}/sprints` | GET | Proje sprintlerini listele |
| `/api/pm/projects/{projectId}/sprints` | POST | Yeni sprint oluştur |
| `/api/pm/sprints/{id}` | GET | Sprint detayı (work items dahil) |
| `/api/pm/sprints/{id}` | PUT | Sprint güncelle |
| `/api/pm/sprints/{id}/start` | POST | Sprint başlat |
| `/api/pm/sprints/{id}/complete` | POST | Sprint tamamla (kalan itemlar?) |

### 2d — Sprint Planlama UI

- Sprint listesi (proje detayında tab)
- Sprint planlama: Backlog → Sprint'e drag & drop
- Sprint board: sadece o sprint'in itemlarını gösteren Kanban
- Sprint kapanışı: tamamlanmayan itemları sonraki sprint'e taşıma dialogu

---

## Faz 3 — Hiyerarşi Kuralları & Backlog View

> **Hedef:** Epic → Feature → UserStory → Task zincirini UI'da destekle.

### 3a — Hiyerarşi Kuralları (Domain)

```csharp
// TaskItemBase.cs'e domain kural metodu ekle
public static class WorkItemHierarchyRules
{
    // Hangi tip hangi tipin altına girebilir?
    private static readonly Dictionary<TaskType, TaskType[]> AllowedChildren = new()
    {
        [TaskType.Epic]      = [TaskType.Feature, TaskType.UserStory],
        [TaskType.Feature]   = [TaskType.UserStory, TaskType.Task, TaskType.Bug],
        [TaskType.UserStory] = [TaskType.Task, TaskType.Bug],
        [TaskType.Task]      = [],  // leaf node
        [TaskType.Bug]       = [TaskType.Task],  // bug fix alt görevleri
        [TaskType.TechDebt]  = [TaskType.Task],
        [TaskType.Spike]     = [TaskType.Task],
    };

    public static bool CanBeChildOf(TaskType childType, TaskType parentType)
        => AllowedChildren.GetValueOrDefault(parentType, []).Contains(childType);
}
```

### 3b — Backlog View API

```text
GET /api/pm/projects/{projectId}/backlog
    ?view=flat|tree           // düz liste veya hiyerarşik ağaç
    &type=Epic,Feature,...    // tip filtre
    &sprint=current|{id}     // sprint filtre
    &assignee={userId}        // atanan kişi filtre
    &status=Backlog,Todo,...  // durum filtre
```

### 3c — Backlog UI

- **Flat View:** Tüm work itemlar düz liste (filtrelenebilir, sıralanabilir)
- **Tree View:** Epic → Feature → Story → Task hiyerarşisi (collapse/expand)
- **Planning View:** Sol: Backlog, Sağ: Sprint → sürükle-bırak
- Work item oluşturma: tip seçimi + parent seçimi
- Inline estimation (Story Points dropdown: 1, 2, 3, 5, 8, 13, 21)

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
    "workItemType": "UserStory",  // veya Epic, Feature, Task
    "projectId": "...",           // hangi projeye eklenecek
    "title": "...",
    "parentWorkItemId": "..."     // opsiyonel — bir Epic/Feature altına ekle
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
    public TaskStatus MappedStatus { get; private set; } // hangi status'a eşlendiği
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

### Adım Adım Migration

```text
1. TaskType enum'ına UserStory, TechDebt, Spike ekle → mevcut değerler korunur
2. TaskItemBase'e StoryPoints, AcceptanceCriteria, SprintId, HierarchyLevel ekle (nullable)
3. Sprint entity oluştur, DbContext'e ekle
4. EF Migration oluştur ve uygula
5. Mevcut TaskItem'lar otomatik HierarchyLevel=0 olur (düz liste gibi davranır)
6. Frontend'de yeni UI bileşenlerini ekle
7. Ticket detayda "İş Kalemi Oluştur" butonunu güncelle
```

### Naming Kararı

| Seçenek | Avantaj | Dezavantaj |
|---------|---------|------------|
| **A) TaskItemBase ismini koru** | Zero breaking change | İsim kavramı yansıtmıyor |
| **B) WorkItemBase olarak rename** | Kavramsal doğruluk | Breaking change (tüm referanslar) |

> [!IMPORTANT]
> **Öneri: Faz 1'de ismi koru (A), tüm fazlar bitince tek seferde rename yap (B).**
> Bu sayede her faz bağımsız test edilebilir.

---

## Dosya Değişiklik Listesi

### Backend

| Dosya | Değişiklik |
|-------|-----------|
| `TaskManagementEnums.cs` | TaskType'a UserStory, TechDebt, Spike ekle; SprintStatus ekle |
| `TaskManagementIds.cs` | SprintId, BoardColumnId ekle |
| `TaskItemBase.cs` | StoryPoints, AcceptanceCriteria, SprintId, HierarchyLevel ekle |
| `SprintBase.cs` | **YENİ** — Sprint entity |
| `BoardColumn.cs` | **YENİ** — Board column entity (opsiyonel, Faz 5) |
| `WorkItemHierarchyRules.cs` | **YENİ** — hiyerarşi doğrulama |
| `TaskManagementDbContext.cs` | Sprint, BoardColumn DbSet + config ekle |
| `TaskManagementQueries.cs` | Backlog tree query, sprint queries ekle |
| `TaskManagementHandlers.cs` | Sprint CRUD handlers, from-ticket handler |
| `TaskManagementEndpoints.cs` | Sprint endpoints, backlog endpoints |

### Frontend

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

| Faz | Başlık | Tahmini Süre | Bağımlılık |
|-----|--------|-------------|-----------|
| 1 | TaskItemBase Evolution (alan + enum) | 0.5 gün | Yok |
| 2 | Sprint Entity & API | 1 gün | Faz 1 |
| 3 | Hiyerarşi Kuralları & Backlog View | 1-2 gün | Faz 1 |
| 4 | Ticket ↔ WorkItem Bağlantı | 1 gün | Faz 1 |
| 5 | Board Geliştirme (WIP, Swimlane) | 1-2 gün | Faz 3 |
| 6 | Velocity & Burndown Charts | 1 gün | Faz 2 |
| | **Toplam** | **~5-8 gün** | |

> [!NOTE]
> Fazlar sıralıdır. Faz 1 tamamlanmadan diğerleri başlanamaz.
> Faz 2 ve 3 birbirinden bağımsız olarak paralel çalışılabilir.
> Süre tahminleri AI-assisted geliştirme ile hesaplanmıştır.

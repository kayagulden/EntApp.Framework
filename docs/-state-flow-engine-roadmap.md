# State Flow Engine — Detaylı Yol Haritası

> **Ana roadmap:** [-roadmap.md (Faz 10b)](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-roadmap.md)
> **Karar:** Elsa 3 yerine hafif, konfigüre edilebilir state machine altyapısı
> **Teknoloji:** [Stateless](https://github.com/dotnet-state-machine/stateless) + [React Flow](https://reactflow.dev) + Dagre auto-layout
> **Tarih:** 2026-04-22

---

## Motivasyon

Elsa 3 ile yaşanan sorunlar:
- Bookmark/resume mekanizması kırılgan (serialization hataları)
- LINQ-to-SQL çeviri sorunları (strongly-typed ID uyumsuzluğu)
- Basit state geçişleri için aşırı karmaşık altyapı (10+ tablo, Activity, WorkflowInstance)
- Blazor WASM designer ayrı bir deployment gerektiriyor
- Camunda gibi alternatifler yüksek maliyetli

**Gerçek ihtiyaç:** State tanımlama, geçiş kuralları, terminal state belirleme — klasik **Finite State Machine (FSM)** problemi.

---

## Mimari Genel Bakış

```
┌─────────────────────────────────────────────────────┐
│           Frontend (Next.js)                         │
│  ┌───────────────────────────────────────────────┐   │
│  │    State Flow Designer (React Flow + Dagre)    │   │
│  │    - Node = State, Edge = Transition           │   │
│  │    - Auto-layout, sürükle-bırak                │   │
│  │    - Properties panel, versiyonlama UI          │   │
│  └───────────────────────────────────────────────┘   │
└──────────────────────┬──────────────────────────────┘
                       │ REST API
┌──────────────────────▼──────────────────────────────┐
│           Backend (.NET)                             │
│  ┌─────────────────┐   ┌──────────────────────────┐ │
│  │ StateFlow Module │   │ Stateless Library        │ │
│  │ (Persistence)    │──▶│ (Runtime Engine)         │ │
│  │ - Definitions    │   │ - Validate transitions   │ │
│  │ - States         │   │ - Execute side effects   │ │
│  │ - Transitions    │   │ - Guard evaluation       │ │
│  │ - Versions       │   └──────────────────────────┘ │
│  └─────────────────┘                                 │
│  ┌─────────────────────────────────────────────────┐ │
│  │ MediatR Events (Side Effects)                   │ │
│  │ - Bildirim gönderme, alan güncelleme, log       │ │
│  └─────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

---

## Aşama A — Domain Model & Persistence

### Entity Modeli

```
StateFlowDefinition (Akış Tanımı)
├── Id: Guid
├── EntityType: string         → "Ticket", "WorkItem", "ChangeRequest"
├── Key: string                → "ticket-default-flow"
├── Name: string               → "Destek Talebi Akışı"
├── Description: string?
├── Version: int               → 1, 2, 3...
├── Status: FlowStatus         → Draft | Published | Archived
├── PublishedAt: DateTime?
├── CreatedAt / UpdatedAt
│
├── States: List<StateDefinition>
│   ├── Id: Guid
│   ├── FlowDefinitionId: FK
│   ├── Name: string           → "New", "WaitForAssignment", "InProgress", "Done"
│   ├── Label: string          → "Yeni", "Atama Bekliyor", "İşlemde", "Tamamlandı"
│   ├── Color: string          → "#3b82f6"
│   ├── Icon: string?          → "circle", "clock", "check"
│   ├── IsInitial: bool        → İlk durum mu?
│   ├── IsTerminal: bool       → Son durum mu? (Done, Cancelled)
│   ├── PositionX: double      → Designer canvas X koordinatı
│   ├── PositionY: double      → Designer canvas Y koordinatı
│   ├── SortOrder: int
│   └── OnEntryActions: string? → JSON: [{"type":"notification","to":"assignee"}]
│
└── Transitions: List<TransitionDefinition>
    ├── Id: Guid
    ├── FlowDefinitionId: FK
    ├── FromStateName: string   → "InProgress"
    ├── ToStateName: string     → "Done"
    ├── TriggerName: string     → "Resolve"
    ├── Label: string           → "Çöz"
    ├── RequiredRole: string?   → "Agent", "Manager"
    ├── GuardExpression: string? → Koşul ifadesi (opsiyonel)
    └── SortOrder: int
```

### DB Schema

- Schema: `sf` (state_flow)
- Tablolar: `state_flow_definitions`, `state_definitions`, `transition_definitions`
- Index: EntityType + Status (Published flow lookup), EntityType + Key + Version (unique)

### Görevler

- [ ] `StateFlow.Domain` — Entity'ler, strongly-typed ID'ler, FlowStatus enum
- [ ] `StateFlow.Application` — Command/Query tanımları (CQRS)
- [ ] `StateFlow.Infrastructure` — DbContext, Handler'lar, EF Configuration
- [ ] Migration SQL dosyası (3 tablo + index'ler)
- [ ] Seed data: Ticket akışı v1 (New → WaitForAssignment → InProgress → Done/Cancelled)

---

## Aşama B — Runtime Engine (Stateless Entegrasyonu)

### Çalışma Prensibi

```
Hiçbir şey bellekte "beklemez"!

1. Entity.Status = "InProgress"     ← DB'de sadece bir string
2. Entity.FlowDefinitionId = v2     ← Hangi versiyonun kuralları geçerli

Geçiş talebi geldiğinde:
   → DB'den entity oku
   → DB'den flow definition oku (entity'nin versiyonuna göre)
   → Stateless machine'i belir (in-memory, μs)
   → Geçişi validate et + uygula
   → Entity'yi kaydet
   → Side effect'leri tetikle (MediatR event)
```

### Görevler

- [ ] NuGet: `Stateless` paketi ekleme
- [ ] `IStateFlowEngine` interface
  - `ValidateTransition(entityType, currentState, trigger, flowDefinitionId)` → bool
  - `GetAllowedTriggers(entityType, currentState, flowDefinitionId)` → List<TriggerInfo>
  - `FireTransition(entityType, currentState, trigger, flowDefinitionId)` → newState
- [ ] `StateFlowEngine` implementasyonu — DB'den tanım yükle, Stateless machine kur
- [ ] Guard desteği — role-based, expression-based
- [ ] Side effect sistemi — OnEntry/OnExit action'ları MediatR event olarak tetikle
- [ ] Ticket modülüne entegrasyon: mevcut hardcoded state geçişlerini StateFlowEngine'e yönlendir
- [ ] WorkItem modülüne entegrasyon

---

## Aşama C — Versiyonlama Sistemi

### Versiyon Yaşam Döngüsü

```
Draft ──[Yayınla]──▶ Published ──[Yeni versiyon yayınlandı]──▶ Archived
                         │
                         ├── Yeni entity'ler bu versiyondan başlar
                         └── Mevcut entity'ler kendi versiyonlarından devam eder
```

### Kurallar

1. **Yeni entity oluşturulduğunda:** `EntityType = X && Status = Published` olan son versiyon bulunur, entity'nin `FlowDefinitionId`'sine atanır
2. **State geçişinde:** Entity'nin kendi `FlowDefinitionId`'sindeki kurallar kullanılır
3. **Publish edildiğinde:** Önceki Published → Archived olur. Mevcut entity'ler etkilenmez
4. **Draft:** Tasarımcı değişiklikleri sadece Draft'ta yapılır, Published akışlar readonly'dir

### Migrasyon Desteği

- [ ] **Güvenli migrasyon:** Yeni versiyona geçerken, eski versiyondaki state'ler yeni versiyonda da mevcutsa, entity otomatik taşınabilir
- [ ] **Uyumluluk kontrolü:** Migrasyon öncesi hangi entity'lerin taşınamayacağı raporlanır
- [ ] Migrasyon seçenekleri:
  - Sadece yeni entity'ler (varsayılan, güvenli)
  - Uyumlu entity'leri de taşı (state eşleşenleri)
  - Tüm entity'leri zorla taşı (dikkatli!)

### Görevler

- [ ] `PublishFlowCommand` — Draft → Published, önceki Published → Archived
- [ ] `CreateNewVersionCommand` — Mevcut Published'dan Draft kopya oluştur
- [ ] `MigrateEntitiesCommand` — Uyumlu entity'leri yeni versiyona taşı
- [ ] Uyumluluk raporu API'si — taşınabilir/taşınamaz entity sayıları

---

## Aşama D — Frontend Designer (React Flow)

### Teknoloji Kararları

| Bileşen | Kütüphane | Neden |
|---------|-----------|-------|
| Graph canvas | `@xyflow/react` (React Flow) | Standart, 20k+ GitHub star, performanslı |
| Auto layout | `dagre` | Basit hiyerarşik düzen, state machine için yeterli |
| State yönetimi | `zustand` (projede mevcut) | React Flow takımının önerisi |

### UI Bileşenleri

```
┌─────────────────────────────────────────────────────────────────┐
│ ⚙️ Ticket Durum Akışı                    v2 (Draft)            │
│ ┌──────────┬────────────────────────────────────────┬──────────┐│
│ │ ARAÇLAR  │              CANVAS                    │ ÖZELLİK  ││
│ │          │                                        │ PANELİ   ││
│ │ ➕ State  │   ┌─────┐ ata  ┌──────────┐           │          ││
│ │          │   │ Yeni ├─────►│ Beklemede │           │ Seçili:  ││
│ │ 🔗 Geçiş │   └─────┘      └────┬─────┘           │ İşlemde  ││
│ │          │              iade │                     │          ││
│ │ 📐 Auto  │        ┌─────────▼─────────┐          │ Etiket:  ││
│ │   Layout │        │     İşlemde       │          │ [İşlemde]││
│ │          │        └──┬──────┬─────────┘          │          ││
│ │ 💾 Kaydet│      çöz  │      │ iptal               │ Renk:    ││
│ │          │   ┌───────▼┐   ┌▼──────┐              │ [🟡]     ││
│ │ 🚀 Yayınla│  │  Done  │   │ İptal │              │          ││
│ │          │   │  ✅    │   │  🚫   │              │ Terminal:││
│ │          │   └────────┘   └───────┘              │ [ ] Evet ││
│ └──────────┴────────────────────────────────────────┴──────────┘│
│ ┌──────────────────────────────────────────────────────────────┐│
│ │ VERSİYONLAR                                                  ││
│ │ v1 Published 12.03.2026 [Aktif: 23 talep]                    ││
│ │ v2 Draft     —          [Düzenleniyor]              [Yayınla]││
│ └──────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### Custom Node Bileşeni (StateNode)

```tsx
// Her state için özel React bileşeni
function StateNode({ data }) {
  return (
    <div className={cn("state-node", data.isTerminal && "terminal")}>
      <div className="state-dot" style={{ background: data.color }} />
      <div className="state-label">{data.label}</div>
      {data.isInitial && <span className="badge">Başlangıç</span>}
      {data.isTerminal && <span className="badge">Son</span>}
      <Handle type="source" position={Position.Right} />
      <Handle type="target" position={Position.Left} />
    </div>
  );
}
```

### Görevler

- [ ] NPM: `@xyflow/react`, `dagre` paketleri ekleme
- [ ] `StateFlowDesigner` ana bileşen (React Flow canvas)
- [ ] `StateNode` custom node bileşeni (renk, ikon, badge)
- [ ] `TransitionEdge` custom edge bileşeni (etiket, silme butonu)
- [ ] Sağ panel: seçili state/transition özellikleri düzenleme
- [ ] Sol toolbar: state ekle, auto-layout, kaydet, yayınla
- [ ] Dagre auto-layout entegrasyonu (tek tıkla otomatik düzenleme)
- [ ] Versiyon listesi ve karşılaştırma UI
- [ ] Migrasyon modalı (taşınabilir entity sayısı, onay)
- [ ] Admin sayfası: `/dashboard/admin/state-flows`

---

## Aşama E — Mevcut Modül Entegrasyonları

### Ticket (Request Management) Entegrasyonu

- [ ] `Ticket` entity'sine `FlowDefinitionId` (nullable FK) ekleme
- [ ] Ticket oluşturulduğunda aktif Published flow'u otomatik atama
- [ ] Mevcut hardcoded status geçişlerini `StateFlowEngine` üzerinden yönlendirme
- [ ] Frontend ticket detay sayfasında: izin verilen geçişleri API'den alma
- [ ] Elsa workflow activity'lerini kademeli olarak devre dışı bırakma

### WorkItem (Project Management) Entegrasyonu

- [ ] `WorkItemBase` entity'sine `FlowDefinitionId` (nullable FK) ekleme
- [ ] Proje oluşturulduğunda varsayılan WorkItem flow'u atama
- [ ] Board/Kanban sürükleme → StateFlowEngine üzerinden validate

### Gelecek Modüller

- [ ] Change Request akışı (16h)
- [ ] Release Management akışı (16e)
- [ ] Gereksinim onay akışı (16c)

---

## Aşama F — İleri Özellikler

### Zamanlayıcı / Otomatik Geçişler

Elsa'nın timer özelliği yerine basit background job:

- [ ] `StateFlowTimer` entity: `FlowDefinitionId`, `FromState`, `Duration`, `AutoTrigger`
- [ ] Hangfire/Quartz recurring job: belirli aralıkla kontrol et, süre dolmuşsa otomatik geçiş
- [ ] Örnek: "WaitingResponse state'inde 3 gün kalırsa → AutoEscalate tetikle"

### Webhook / Event Entegrasyonu

- [ ] Geçişte dış sisteme webhook gönderme
- [ ] Geçiş sonrası MediatR integration event yayınlama
- [ ] Audit log: tüm geçişler kaydedilir (kim, ne zaman, ne yaptı)

### Raporlama

- [ ] State distribution raporu (hangi state'te kaç entity var)
- [ ] Ortalama state süresi (lead time, cycle time)
- [ ] Tıkanma noktası analizi

---

## Elsa 3 Geçiş Planı

| Aşama | Durum | Açıklama |
|-------|-------|----------|
| **Mevcut** | Elsa 3 aktif | Ticket workflow Elsa üzerinden çalışıyor |
| **Aşama A-B** | Paralel geliştirme | StateFlow modülü geliştirilir, Elsa'ya dokunulmaz |
| **Aşama E** | Kademeli geçiş | Ticket state geçişleri StateFlowEngine'e yönlendirilir |
| **Son** | Elsa devre dışı | Elsa dependency'leri kaldırılır, tabloları archived |

> [!WARNING]
> Elsa 3 kaldırılmadan önce, Elsa üzerinden çalışan mevcut workflow instance'ları tamamlanmalı veya migrate edilmelidir.

---

## Teknoloji Karşılaştırma Tablosu

| Özellik | Elsa 3 | StateFlow Engine |
|---------|--------|-----------------|
| State tanımlama | Activity + Flowchart | DB + Designer UI |
| Geçiş kuralları | Elsa Activities | Transition tablosu |
| Runtime | Workflow Instance (serialized) | In-memory Stateless (stateless) |
| Uzun süreli bekleme | Bookmark (kırılgan) | DB field (sağlam) |
| Versiyonlama | Var (karmaşık) | Var (basit, FK tabanlı) |
| Designer | Blazor WASM (ayrı deployment) | React Flow (entegre) |
| DB yükü | 10+ tablo | 3 tablo |
| Performans | Ağır (instance yönetimi) | Hafif (μs düzeyinde) |
| Hata ayıklama | Zor | Kolay |
| Zamanlayıcı | Yerleşik timer activity | Background job (Hangfire) |

---

## Tahmini Süre

| Aşama | Süre | Öncelik |
|-------|------|---------|
| A — Domain & Persistence | 2-3 gün | P0 |
| B — Runtime Engine | 2-3 gün | P0 |
| C — Versiyonlama | 1-2 gün | P0 |
| D — Designer UI | 3-5 gün | P1 |
| E — Modül Entegrasyonları | 2-3 gün | P1 |
| F — İleri Özellikler | 2-3 gün | P2 |
| **Toplam** | **~12-19 gün** | |

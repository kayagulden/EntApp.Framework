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

## Mimari Karar: Status = String + Semantic Flags

> **Karar tarihi:** 2026-04-22

State'ler entity'lerde **enum olarak değil, string olarak** saklanır. StateFlow tanımı **tek kaynak (single source of truth)** olur.

### Neden?

| Yaklaşım | Sorun |
|-----------|-------|
| Enum kalır, flow sadece geçiş kural tanımlar | Yeni state = code change + rebuild + redeploy → Designer anlamsız olur |
| Enum kalkar, her yer raw string | Type safety yok, `if (status == "Done")` → typo riski |
| **✅ String + Semantic Flags** | Dinamik state ekleme + flag'lerle anlamsal kontrol |

### Kural: Kod asla state ADINI kontrol etmez

```csharp
// ❌ YANLIŞ
if (ticket.Status == "Done") { StopSlaTimer(); }

// ✅ DOĞRU — semantic flag kontrolü
var stateDef = flow.States.First(s => s.Name == ticket.Status);
if (stateDef.IsTerminal) { StopSlaTimer(); }
```

### Semantic Flag'ler

| Flag | Tip | Anlamı | Kod davranışı |
|------|-----|--------|--------------|
| `IsInitial` | bool | Başlangıç state'i | Yeni entity oluşturulduğunda bu state atanır |
| `IsTerminal` | bool | Bitiş state'i | SLA durdur, resolved say, raporlarda "kapalı" |
| `Category` | string | Gruplama | `Active` / `Waiting` / `Closed` → dashboard, filtreleme |
| `IsPaused` | bool | Beklemede mi? | SLA saatini duraklat (SLA timer freeze) |

### Migration: Enum → String

```sql
-- Mevcut enum değerleri zaten string'e dönüştürülebilir
-- TicketStatus.InProgress.ToString() == "InProgress"
ALTER TABLE req.tickets ALTER COLUMN "Status" TYPE varchar(50);
```

---

## Potansiyel Riskler & Koruma Önlemleri

### Risk 1: Admin yanlışlıkla aktif state'i siler

```
"InProgress" state'inde 15 talep var → Admin state'i silmek istedi
```

**Koruma:**
- Published flow readonly'dir, sadece Draft'ta değişiklik yapılabilir
- Publish öncesi validasyon: silinen state'te aktif entity var mı kontrol et
- Varsa uyarı göster: "Bu state'te 15 talep var. Önce bu talepleri başka state'e taşıyın."

### Risk 2: State adı değiştirildi, mevcut entity'ler orphan kalır

```
"InProgress" → "Working" olarak yeniden adlandırıldı
Ama DB'de hala Status = "InProgress" olan entity'ler var
```

**Koruma:**
- State adı değiştirme = yeni versiyon gerektirir (mevcut state silinir + yeni eklenir)
- Publish sırasında uyumluluk raporu: "Eski versiyondaki 'InProgress' state'ine karşılık yeni versiyonda state bulunamadı"
- Admin mapping tanımlar: `InProgress → Working` (migrasyon sırasında otomatik güncellenir)

### Risk 3: Flow tanımı olmadan entity oluşturma

```
EntityType = "Ticket" için Published flow yok → Ticket oluşturulamaz
```

**Koruma:**
- `CreateTicket` handler'ı Published flow yoksa hata döner: "Bu entity tipi için aktif akış tanımı bulunamadı"
- Seed data ile varsayılan akışlar otomatik oluşturulur (Ticket, WorkItem)
- Admin panelde uyarı: "Yayınlanmış akış yok, yeni talep oluşturulamaz"

### Risk 4: Performans — her state geçişinde DB'den flow yükleme

```
Her geçişte: Flow + States + Transitions → 3 tablo join
```

**Koruma:**
- Flow tanımları nadiren değişir → **In-memory cache** (IMemoryCache, 5 dk TTL)
- Publish/Archive olduğunda cache invalidate
- Flow yükleme zaten hafif: ~3-10 state, ~5-15 transition = küçük veri seti

### Risk 5: String karşılaştırmada büyük-küçük harf uyumsuzluğu

```
DB: Status = "inprogress" vs Flow state: "InProgress"
```

**Koruma:**
- State name'ler her zaman **PascalCase** zorunluluğu (validation)
- DB'ye yazarken ve flow'dan okurken `StringComparison.OrdinalIgnoreCase` kullanma
- Alternatif: state name'ler slug formatında (`in-progress`) — typo riski azalır

### Risk 6: Birden fazla Published flow aynı EntityType için

```
EntityType = "Ticket" için 2 tane Published flow var → hangisi kullanılacak?
```

**Koruma:**
- DB constraint: `UNIQUE(EntityType, Status) WHERE Status = 'Published'` (partial unique index)
- Publish komutu önceki Published'ı otomatik Archived yapar
- Uygulama seviyesi validasyon: PublishFlowCommand içinde kontrol

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
│   ├── IsPaused: bool         → Beklemede mi? (SLA freeze)
│   ├── Category: string       → "Active", "Waiting", "Closed"
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

- [x] `StateFlow.Domain` — Entity'ler, strongly-typed ID'ler, FlowStatus enum
- [x] `StateFlow.Application` — Command/Query tanımları (CQRS)
- [x] `StateFlow.Infrastructure` — DbContext, Handler'lar, EF Configuration
- [x] Migration SQL dosyası (3 tablo + index'ler)
- [x] Seed data: Ticket akışı v1 (New → WaitForAssignment → InProgress → Done/Cancelled)

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

- [x] NuGet: `Stateless` paketi ekleme
- [x] `IStateFlowEngine` interface
  - `ValidateTransition(entityType, currentState, trigger, flowDefinitionId)` → bool
  - `GetAllowedTriggers(entityType, currentState, flowDefinitionId)` → List<TriggerInfo>
  - `FireTransition(entityType, currentState, trigger, flowDefinitionId)` → newState
- [x] `StateFlowEngine` implementasyonu — DB'den tanım yükle, Stateless machine kur
- [ ] Guard desteği — role-based, expression-based
- [ ] Side effect sistemi — OnEntry/OnExit action'ları MediatR event olarak tetikle
- [x] Ticket modülüne entegrasyon: mevcut hardcoded state geçişlerini StateFlowEngine'e yönlendir
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

- [x] `PublishFlowCommand` — Draft → Published, önceki Published → Archived
- [x] `CreateNewVersionCommand` — Mevcut Published'dan Draft kopya oluştur
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

- [x] NPM: `@xyflow/react`, `dagre` paketleri ekleme
- [x] `StateFlowDesigner` ana bileşen (React Flow canvas + ReactFlowProvider)
- [x] `StateNode` custom node bileşeni (renk, ikon, badge, inline styles)
- [x] `TransitionEdge` custom edge bileşeni (etiket, label renderer)
- [x] Sağ panel: seçili state/transition özellikleri düzenleme (PropertiesPanel)
- [x] Sol toolbar: state ekle, auto-layout, kaydet, yayınla
- [x] Dagre auto-layout entegrasyonu (tek tıkla otomatik düzenleme)
- [ ] Versiyon listesi ve karşılaştırma UI
- [ ] Migrasyon modalı (taşınabilir entity sayısı, onay)
- [x] Admin sayfası: `/dashboard/admin/state-flows` (liste + designer)

---

## Aşama E — Mevcut Modül Entegrasyonları

### Ticket (Request Management) Entegrasyonu

- [x] `Ticket` entity'sine `FlowDefinitionId` (nullable FK) ekleme
- [x] Ticket oluşturulduğunda aktif Published flow'u otomatik atama
- [x] Mevcut hardcoded status geçişlerini `StateFlowEngine` üzerinden yönlendirme
- [x] Frontend ticket detay sayfasında: izin verilen geçişleri API'den alma (endpoint hazır)
- [x] Queue routing: Elsa RouteToQueueActivity yerine kategori bazlı otomatik routing
- [x] WaitForAssignment: Elsa blocking activity yerine StateFlow state olarak modellendi
- [x] Elsa bağımlılığı RM modülünden tamamen kaldırıldı (IWorkflowStarter, Elsa.Workflows.Runtime NuGet)

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
| **Mevcut** | ✅ Elsa tamamen kaldırıldı | Tüm Elsa NuGet, konfigürasyon, activity, endpoint ve frontend sayfaları temizlendi |
| **Aşama A-B** | ✅ Tamamlandı | StateFlow modülü + runtime engine geliştirildi |
| **Aşama C** | ✅ Tamamlandı | Versiyonlama (Publish/Archive/NewVersion) hazır |
| **Aşama D** | ✅ Tamamlandı | React Flow designer çalışıyor |
| **Aşama E** | 🔶 Kısmen tamamlandı | Ticket entegrasyonu ✅. WorkItem entegrasyonu bekliyor |
| **Son** | ✅ Tamamlandı | Elsa tamamen kaldırıldı (backend + frontend + designer workflow) |

> [!NOTE]
> Elsa 3 tamamen projeden kaldırılmıştır. DB'deki `"Elsa"` şeması mevcut verileri korumak için bırakılabilir veya manuel olarak `DROP SCHEMA "Elsa" CASCADE` ile temizlenebilir.

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

# 📋 Task, Talep (Ticket) & Workflow — Karar Özeti ve Yapılacaklar

> **Son güncelleme:** 2026-04-14
> **Kapsam:** 10+ konuşmada alınan tüm mimari kararlar, tamamlanan işler ve henüz yapılmayan maddeler

---

## 1. Kararlaştırılan Mimari

### 1.1 Talep (Ticket) vs Görev (Task) İlişkisi

```
Talep = "Ne yapılması isteniyor?" → Müşteri/kullanıcı perspektifi  
Görev = "Bunu yapmak için ne yapılması gerekiyor?" → Çalışan perspektifi
```

| Senaryo | Talep | Görevler |
|---------|-------|----------|
| Basit | "Şifremi sıfırlayın" | 1 görev — şifreyi sıfırla |
| Orta | "Yeni kullanıcı oluşturun" | 2 görev — AD hesabı + e-posta |
| Karmaşık | "Yeni modül geliştirin" | N görev — analiz, tasarım, test... |
| **Bağımsız** | _(talep yok)_ | "Haftalık rapor hazırla", "Sunucuyu güncelle" |

**Verilen karar:** **Yaklaşım A — Loose Coupling** (Integration Event tabanlı)
- İki modül bağımsız kalır (`RequestManagement` ↔ `TaskManagement`)
- Aralarındaki bağlantı: **polimorfik referans** (`SourceModule`, `SourceType`, `SourceId`)
- Direkt FK yerine integration event'ler ile iletişim

### 1.2 Görev Türleri

```
TaskItem
├── 🔗 Talep Bağlı  (SourceModule="RequestManagement", SourceId=TicketId)
├── 📁 Proje Bağlı   (ProjectId != null)
└── 🆓 Bağımsız     (SourceId=null, ProjectId=null)
```

**Verilen karar:** `ProjectId` **nullable** yapıldı — bağımsız görevler proje olmadan yaşayabilir.

### 1.3 Görev Oluşturma Mekanizması

**Verilen karar:** **Hibrit** (C seçeneği)
- Workflow otomatik olarak başlangıç görevlerini oluşturur (`CreateTaskForAssignee` activity)
- Claim eden kişi **manuel** ek görevler ekleyebilir (ticket detay → Görevler paneli)

### 1.4 Tamamlanma Kuralı

**Verilen karar:** **B seçeneği** — Talep `AllTasksDone` ara durumuna geçer
- Tüm görevler Done/Cancelled olunca → `AllSourceTasksCompletedEvent` publish
- Ticket otomatik `AllTasksDone` statüsüne geçer
- Workflow bu event'i catch edip sonraki adıma (Resolved) geçer

### 1.5 Elsa Workflow = Uçtan Uca Yaşam Döngüsü

```
Workflow ≠ tek queue'daki süreç
Workflow = talebin başlangıçtan bitişe TÜM yaşam döngüsü
         → birden fazla queue'yu orkestre edebilir
```

| Kavram | Tanım | Örnek |
|--------|-------|-------|
| **Workflow** | Uçtan uca süreç | "Laptop Talebi" — satınalma → kurulum → teslimat |
| **Queue** | Bir aşamadaki ekip | "IT Destek", "Satınalma Ekibi" |
| **RouteToQueue** | Aktivite — ticket'ı sonraki queue'ya taşır | Satınalma bitti → IT Kurulum'a |
| **Kategori** | Workflow'u belirleyen bağlayıcı | "Laptop Talebi" → "laptop-akisi" workflow |

**Workflow bağlama:** `RequestCategory.WorkflowDefinitionId` — **sadece kategori bazlı**

### 1.6 Workflow Aksiyonları (Frontend'deki Dinamik Butonlar)

> **ÖNEMLİ: Bu kısım henüz implemente EDİLMEDİ, sadece karar alındı.**

Ticket detay sayfasında statik "Durum Değiştir" dropdown'u yerine **workflow-driven dinamik aksiyonlar** gösterilecek:

```
Mevcut (statik):          Hedef (dinamik):
┌──────────────────┐      ┌──────────────────┐
│ Durum: [Dropdown]│      │ 🟢 Onayla        │ ← WaitForApproval'dan
│ [Uygula]         │      │ 🔴 Reddet        │ ← WaitForApproval'dan
└──────────────────┘      │ 💬 Bilgi İste    │ ← workflow opsiyonel geçiş
                          │ ⬆️ Eskale Et     │ ← workflow opsiyonel geçiş
                          └──────────────────┘
```

**Mekanizma:**
1. Elsa'da `WaitForApproval` aktivitesi bir **bookmark** oluşturur
2. Frontend `GET /api/req/tickets/{id}/actions` ile bookmark'ları sorgular
3. Her bookmark → bir buton olarak gösterilir (label + outcome'lar)
4. Kullanıcı butona tıklayınca `POST /api/req/tickets/{id}/actions/{actionId}` → bookmark resume

### 1.7 Bağımsız Görev Sayfalarının Yeri

**Verilen karar:** Ayrı `/dashboard/tasks` sayfası, sidebar'da "Kişisel" bölümünde
- Talepler sayfasına tab ekleme fikri **reddedildi** (domain karışıklığı, bağımsız görevler orada garip)
- Queue Lead → tüm member görevlerini görsün + başkalarına atama yapabilsin
- Queue Member → sadece kendi görevlerini görsün

---

## 2. ✅ Tamamlanan İşler

### 2.1 Backend — Elsa Custom Activities (10 adet)

| Aktivite | Tip | Açıklama |
|----------|-----|----------|
| `ChangeTicketStatus` | CodeActivity | Ticket durumunu değiştirir |
| `AssignTicket` | CodeActivity | Ticket'ı bir kullanıcıya atar |
| `RouteToQueue` | CodeActivity | Ticket'ı bir kuyruğa yönlendirir |
| `SendNotification` | CodeActivity | Bildirim gönderir |
| `AddComment` | CodeActivity | Ticket'a yorum ekler |
| `GetTicketDetails` | CodeActivity | Ticket bilgilerini workflow değişkenlerine yükler |
| `CheckSLA` | CodeActivity | SLA kontrolü yapar |
| `CreateTaskForAssignee` | CodeActivity | Ticket assignee'si için otomatik görev oluşturur |
| **`WaitForApproval`** | **Blocking** | Workflow duraklar, kullanıcı Onayla/Reddet bekler |
| **`WaitForAllTasksDone`** | **Blocking** | Workflow duraklar, tüm görevler bitene kadar bekler |

### 2.2 Backend — Integration Events

| Event | Yön | Açıklama |
|-------|-----|----------|
| `TaskCreatedForSourceEvent` | TM → RM | Talebe görev oluşturuldu |
| `TaskStatusChangedEvent` | TM → RM | Görev durumu değişti |
| `AllSourceTasksCompletedEvent` | TM → WF | Tüm görevler tamamlandı → Elsa bookmark resume |
| `TicketCreatedEvent` | RM → WF | Talep oluşturuldu → Workflow otomatik başlatma |

### 2.3 Backend — Ticket-Task Entegrasyonu

- `Ticket.LinkedTaskCount` / `CompletedTaskCount` alanları
- `Ticket.MarkAllTasksDone()` metodu  
- `TicketTaskEventHandler` — `TaskCreatedForSourceEvent` ve `AllSourceTasksCompletedEvent` handler'ları
- `WorkflowTaskCompletionHandler` — `AllSourceTasksCompletedEvent` → Elsa bookmark resume
- `TicketStatus.AllTasksDone = 9` enum değeri

### 2.4 Backend — Task CRUD

- `TaskItemBase.Update()` metodu
- `UpdateTaskCommand` + `UpdateTaskCommandHandler` + `PUT /api/pm/tasks/{id}` endpoint
- `ListTasksQuery` genişletildi: `ReporterUserId`, `AssigneeUserIds`, `Type`, `SourceFilter`
- `TaskDetailDto` (zengin response: project key, source bilgileri, sub-tasks)

### 2.5 Backend — Claim Ticket (Üzerime Al)

- `ClaimTicketCommand` + `ClaimTicketHandler` → `POST /api/req/tickets/{id}/claim`
- Queue üyelik kontrolü (ticket'ın queue'sundaki üye olmalı)
- Frontend: tickets sayfasından "Üzerime Al" butonu (`handleClaim`)

### 2.6 Backend — Kategori → Workflow Otomatik Başlatma

- `CreateTicketHandler` içinde kategori'nin `WorkflowDefinitionId`'si kontrol ediliyor
- Varsa: `IWorkflowStarter.StartWorkflowAsync()` ile Elsa workflow başlatılıyor
- Workflow input: `TicketId`, `CategoryId`, `DepartmentId`, `Priority`, `Channel`
- Ticket'a `LinkWorkflow(instanceId)` ile workflow bağlanıyor
- Yoksa: Fallback olarak `DefaultQueueId` ile kuyruk routing yapılıyor

### 2.7 Frontend — Elsa Workflow Designer

- `/manage/workflows` — Workflow listesi  
- `/manage/workflows/[id]` — Elsa Studio Designer (iframe embed)
- "Destek Talebi Akışı" workflow seed edildi

### 2.8 Frontend — Ticket (Talep) Sayfaları

- `/dashboard/tickets` — 3 tab'lı liste (Taleplerim, Üzerimde, Kuyruk Havuzu) + Claim butonu
- `/dashboard/tickets/[id]` — Detay sayfası (görev listesi, yorum, atama, durum değiştirme)
- Ticket detayda görev oluşturma + listeleme (by-source endpoint)

### 2.9 Frontend — Task (Görev) Sayfaları

- `/dashboard/tasks` — 3 tab'lı liste (Üzerimdeki, Oluşturduklarım, Kuyruk Görevleri)
- `/dashboard/tasks/[id]` — Detay sayfası (düzenleme, alt görevler, yorumlar, zaman girişleri)
- Sidebar → "Görevlerim" menü öğesi

---

## 3. ❌ Henüz Yapılmayan İşler

### 🔴 Yüksek Öncelik

#### 3.1 Dinamik Aksiyonlar Paneli (Workflow-Driven)
> **Ne:** Ticket detayda statik dropdown yerine workflow'dan gelen dinamik butonlar  
> **Neden Lazım:** Elsa workflow çalışıyor ama frontend'e yansımıyor — kullanıcı "Onayla/Reddet" yapamıyor  
> **Gerekli İşler:**
- [ ] Backend: `GET /api/req/tickets/{id}/actions` — Elsa'dan aktif bookmark'ları sorgula, buton listesi dön
- [ ] Backend: `POST /api/req/tickets/{id}/actions/{actionId}` — Elsa bookmark resume et
- [ ] Frontend: `tickets/[id]/page.tsx` → aksiyonlar panelini workflow-driven yap
- [ ] Fallback: Workflow bağlı olmayan ticket'lar için basit enum-based durum geçişi kalsın

#### 3.2 Unclaim ("Havuza Bırak")
> **Claim ✅ var — Unclaim yok**
> **Gerekli İşler:**
- [ ] Backend: `UnclaimTicketCommand` + handler + `POST /api/req/tickets/{id}/unclaim` endpoint
- [ ] Frontend: Ticket detayda "Havuza Bırak" butonu (sadece assignee isen gösterilir)

#### 3.3 Departman/Kategori Bazlı Dinamik Form
> **Ne:** `RequestCategory.FormSchemaJson` alanındaki JSON schema ile talep oluşturma formunda ek alanlar gösterme  
> **Roadmap Notu:** "_16a öncesi yapılmalı_" denmiş ama henüz yapılmadı  
> **Gerekli İşler:**
- [ ] Frontend: `SchemaForm` component'ini ticket oluşturma formuna entegre et
- [ ] Backend: FormDataJson'u ticket create/update akışında sakla (kısmen mevcut)

### 🟡 Orta Öncelik

#### 3.4 Triage / Dispatcher Akışı
> **Ne:** Gelen talepleri sınıflandırma, kategori atama, kuyruğa route etme  
> **Gerekli İşler:**
- [ ] Backend: Dispatcher endpoint'leri (talep kategorisi güncelleme + yeniden route)
- [ ] Frontend: Dispatcher görünümü

#### 3.5 Görev Silme (DeleteTask)
> **Ne:** Task silme command/endpoint yok  
> **Gerekli İşler:**
- [ ] Backend: `DeleteTaskCommand` + handler + `DELETE /api/pm/tasks/{id}` endpoint

#### 3.6 Bildirim Entegrasyonu
> **Ne:** Ticket/Task değişikliklerinde in-app + e-posta bildirim  
> **Gerekli İşler:**
- [ ] `SendNotification` activity'sini Notification modülü ile bağla
- [ ] Frontend: bildirim panelinde ticket/task event'leri göster

### 🟢 Düşük Öncelik (Gelecek Fazlar)

#### 3.7 SLA İzleme Dashboard / UI
- [ ] Frontend: SLA dashboard sayfası
- [ ] Backend: SLA istatistik endpoint'leri

#### 3.8 Timer Activities (Otomatik SLA Eskalasyon)
- [ ] Elsa timer/scheduled activity'ler — SLA breach yaklaşınca otomatik eskalasyon

#### 3.9 Workflow Şablonları
- [ ] Hazır workflow template'leri seed data olarak ekle (Standart IT, VPN, İzin)

#### 3.10 Proje & Portfolio Yönetimi (Faz 16b)
- [ ] Sprint, burndown, Kanban drag-drop, WIP limit (Roadmap Faz 16b)

#### 3.11 Organizasyon Yönetim Sayfası
- [ ] `/manage/organizations` — ağaç görünümü, departman ekleme/düzenleme

---

## 4. Referans: Mevcut Dosya Haritası

### Backend
| Dosya | Konum |
|-------|-------|
| Elsa Activities (10) | `src/Modules/Workflow/EntApp.Modules.Workflow.Infrastructure/Activities/` |
| Task Entity | `src/Modules/TaskManagement/.../Domain/Entities/TaskItemBase.cs` |
| Task Commands | `src/Modules/TaskManagement/.../Application/Commands/TaskManagementCommands.cs` |
| Task Queries | `src/Modules/TaskManagement/.../Application/Queries/TaskManagementQueries.cs` |
| Task Endpoints | `src/Modules/TaskManagement/.../Infrastructure/Endpoints/TaskManagementEndpoints.cs` |
| Integration Events | `src/Modules/TaskManagement/.../Application/IntegrationEvents/TaskManagementEvents.cs` |
| Ticket Entity | `src/Modules/RequestManagement/.../Domain/Entities/Ticket.cs` |
| Ticket Handlers | `src/Modules/RequestManagement/.../Infrastructure/Handlers/RequestManagementHandlers.cs` |
| Ticket-Task Handler | `src/Modules/RequestManagement/.../Infrastructure/Handlers/TicketTaskEventHandler.cs` |

### Frontend
| Sayfa | Konum |
|-------|-------|
| Talep Listesi | `src/Frontend/entapp-web/src/app/dashboard/tickets/page.tsx` |
| Talep Detay | `src/Frontend/entapp-web/src/app/dashboard/tickets/[id]/page.tsx` |
| Görev Listesi | `src/Frontend/entapp-web/src/app/dashboard/tasks/page.tsx` |
| Görev Detay | `src/Frontend/entapp-web/src/app/dashboard/tasks/[id]/page.tsx` |
| Workflow Designer | `src/Frontend/entapp-web/src/app/manage/workflows/[id]/page.tsx` |

### Dokümantasyon
| Doküman | İçerik |
|---------|--------|
| `docs/elsa-workflow-integration.md` | Elsa entegrasyon planı, custom activities, faz planı |
| `docs/REQUEST_MANAGEMENT_ROADMAP.md` | Request Management ilerleme durumu |
| `docs/_roadmap.md` | Tüm proje roadmap'i (Faz 1-16) |
| **`docs/task-ticket-workflow-summary.md`** | **Bu doküman** |

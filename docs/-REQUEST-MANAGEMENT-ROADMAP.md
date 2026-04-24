# 16a — Request Management (Talep Yönetimi) — Detaylı Yol Haritası

> **Ana roadmap:** [-roadmap.md (Faz 16)](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-roadmap.md#faz-16--delivery-platform-almitsm-mod�lleri)
> **Backend tamamlanma:** 2026-04-05
> **Workflow entegrasyonu:** 2026-04-17

---

## ✅ Tamamlanan Maddeler

### Backend Core
- [x] `Department`, `RequestCategory`, `SlaDefinition`, `Ticket`, `TicketComment`, `TicketStatusHistory` entity'leri
- [x] Talep oluşturma + SLA takibi (`SlaCalculator`, otomatik deadline hesaplama)
- [x] Sequential ticket numarası (`TicketNumberGenerator` — REQ-0001 formatı)
- [x] CQRS: 12 command, 8 query, 5 validator, 20 handler, 20 API endpoint (`/api/req/`)
- [x] Integration Events: `TicketCreatedEvent`, `TicketAssignedEvent`, `TicketSlaBreachedEvent`, `TicketResolvedEvent`
- [x] `RequestManagementDbContext` (schema: `req`, 6 tablo)
- [x] Dinamik form altyapısı (FormSchemaJson, SchemaForm, FormSchemaBuilder)

### Kuyruk Yönetimi
- [x] ServiceQueue + QueueMembership entity'leri ve strongly-typed ID'ler
- [x] DbContext (2 tablo: `service_queues`, `queue_memberships`)
- [x] CQRS: 5 Command + 2 Query + 7 Handler
- [x] 7 API Endpoint (`/api/req/queues`)
- [x] DTO projeksiyonu (circular JSON reference düzeltmesi)
- [x] IAM'den kullanıcı adı çözümleme (batch user lookup)

### Ticket → Queue Routing
- [x] Kategori seçildiğinde ticket otomatik olarak ilgili kuyruğa yönlendirilsin
- [x] Kategori ↔ Queue eşlemesi (DefaultQueueId üzerinden otomatik)
- [x] RouteToQueue activity — Elsa workflow'dan programatik route

### Claim (Üzerime Al)
- [x] Backend: `ClaimTicketCommand` + `ClaimTicketHandler` (kuyruk üyelik validasyonu, idempotent)
- [x] Backend: `AssignTicketCommand` + `AssignTicketHandler`
- [x] Backend: `TicketAssignedEvent` integration event
- [x] Frontend: Ticket detayında "Üzerime Al" butonu (workflow aksiyonlar panelinden)
- [x] Üzerime Al sonrası otomatik status değişimi (New → InProgress)
- [x] Üzerime Al sonrası UI otomatik güncelleme (delay + refreshAll)

### Workflow Entegrasyonu (Elsa v3)
- [x] Kategori bazlı workflow tanımı (RequestCategory.WorkflowDefinitionId)
- [x] Talep oluşturulduğunda otomatik workflow başlatma
- [x] Custom blocking activities: WaitForAssignment, WaitForStatusDecision, WaitForAllTasksDone
- [x] RouteToQueue activity — otomatik kuyruk yönlendirmesi
- [x] Ticket detay sayfasında dinamik aksiyonlar paneli (bookmark-driven)
- [x] Workflow bookmark resume mekanizması (doğrudan bookmarkId ile)
- [x] Elsa Designer entegrasyonu (Blazor WASM iframe)
- [x] Programatik workflow seed ("Destek Talebi Akışı")

### Görev Entegrasyonu
- [x] Bağımsız görev yönetimi (TaskManagement modülü)
- [x] Ticket'a bağlı görev oluşturma (from-source)
- [x] WaitForAllTasksDone activity — tüm görevler bitene kadar bekle
- [x] Denormalized görev sayaçları (LinkedTaskCount, CompletedTaskCount)

### Frontend
- [x] `/manage/queues` — master-detail, create modal, departman dropdown, üye yönetimi
- [x] Sidebar → Talep Yönetimi → Hizmet Kuyrukları
- [x] `/dashboard/tickets` — talep listesi sayfası

### Taleplerim Sayfası (Talep Sahibi Görünümü)
- [x] Kullanıcının kendi oluşturduğu talepleri listesi (`/dashboard/tickets` → "Taleplerim" sekmesi)
- [x] Durum takibi, detay görüntüleme (`/dashboard/tickets/[id]`)
- [x] Yorum bırakabilme (ticket detay sayfasında)

### Organizasyon Yönetim Sayfası
- [x] `/manage/organizations` — 3 panelli ağaç görünümü (org tree, departman listesi, departman detayı)
- [x] Organizasyon ekleme (API bağlantılı)
- [x] Departman ekleme/düzenleme (yönetici, üst departman, varsayılan kuyruk)

### Onay Akışı Altyapısı
- [x] `WaitForApprovalActivity` — configurable outcome'lu blocking activity
- [x] Bookmark payload (`ApprovalBookmarkPayload`) — ticketId, label, outcomes, approverUserId
- [x] Frontend aksiyonlar paneli — ticket detayında Onayla/Reddet butonları otomatik render
- [x] "Bekleyen Onaylar" sayfası (`/dashboard/approvals`) — UI mevcut

### Seed Data
- [x] Demo şirket (EntApp Demo) + 2 şube + 6 IAM departman
- [x] 5 demo kullanıcı (org+dept atanmış)
- [x] 4 Request departmanı + 8 hizmet kuyruğu + 13 üyelik + 8 kategori

---

## 📋 Devam Edilecek Maddeler

### Onay Akışları (Tamamlanması Gereken)
> **Not:** Organizasyon Yönetim Sayfası'ndan sonra implemente edilecek — kullanıcı-yönetici ilişkisi onay akışının ön koşuludur.

- [ ] `RequestCategory.RequiresApproval: bool` — kategori bazlı onay zorunluluğu
- [ ] `ResolveApproverActivity` — talebi açanın yöneticisini IAM User.ManagerUserId üzerinden çözen Elsa activity
- [ ] Workflow akışı: `ResolveApprover → WaitForApproval → (Approved: RouteToQueue) / (Rejected: Cancel)`
- [ ] "Bekleyen Onaylar" sayfasının Elsa bookmark API'sine bağlanması
- [ ] Onaylayıcı çözümleme: `IAM.User.ManagerUserId` → talebi açanın yöneticisi dinamik olarak bulunur
- [ ] Herhangi bir onaylayıcının onayı yeterli (tek onay mantığı)

### Dinamik Form & Talep Olgunlastirma (Intake)
- [ ] Departman/kategori bazli dinamik form (RequestCategory.FormSchema -> Dynamic UI)
- [ ] Talep olgunlastirma formu: Proje/Ozellik Talebi kategorilerinde kapsam, etki analizi, butce, sponsor onayi, is gerekcesi gibi yapisal alanlar
- [ ] `Ticket.IntakeFormJson` -- kategori formuna gore doldurulan JSON veri (esnek yapi)
- [ ] StateFlow'da 'Detaylandirma Bekliyor' durumu -- kullanicidan intake form doldurmasi istenir
- [ ] 'Projeye Aktar' sirasinda intake bilgilerini Proje aciklamasina ve FeatureSpec gereksiniminin Description alanina otomatik Markdown donusumu

### Workflow Görev Oluşturma
- [ ] `CreateTaskForAssignee` activity ile workflow'dan otomatik görev oluşturma (activity mevcut, workflow akışına eklenmedi)

### Talep Sahibi Portalı
- [ ] Self-service UI — talep sahibinin kendi taleplerini takip edebileceği portal

### Küçük Bekleyen Maddeler
- [ ] ~Ticket düzenleme imkanı~ — şimdilik gerekli değil, ihtiyaç olursa eklenebilir
- [ ] Yönetici atama dropdown'u — Keycloak aktif olduğunda IAM users API üzerinden çalışacak

---

## 🛑 Ertelenenler

### Unclaim (Havuza Bırak)
- [ ] Elsa v3'ün teknik yetersizlikleri ve döngüsel state kısıtlamaları nedeniyle şimdilik rafa kaldırıldı.
- [ ] Backend (`UnclaimTicketCommand`) ve Frontend servisleri hazır, ancak Workflow (WaitForAssignment) entegrasyonu bekliyor.

### Triage / Dispatcher Akışı
- [ ] Mevcut sistemde kuyruk yönlendirmesi workflow (RouteToQueue activity) ve kategori bazlı otomatik routing ile yapılıyor, ayrı bir Dispatcher akışına şu an ihtiyaç yok.
- [ ] **Workflow uyumsuzluk riski:** Dispatcher bir ticket'ı manuel olarak farklı bir kuyruğa yönlendirdiğinde, workflow'daki RouteToQueue activity'si ile çakışma yaratabilir. Workflow kuyruğu A olarak belirlemiş ama Dispatcher B'ye taşımışsa, workflow state'i tutarsız hale gelebilir.
- [ ] İleride ihtiyaç olursa, ticket detayında basit bir "Kuyruğu Değiştir" dropdown'u (backend API zaten hazır: `POST /tickets/{id}/route`) yeterli olabilir — ancak workflow uyumluluğu önce analiz edilmeli.

### Paralel Onay Akışı
- [ ] Birden fazla onaylayıcının aynı anda onaylaması gereken senaryolar (ör: 3 kişiden 2'si onaylarsa geç).
- [ ] Yeni bir `WaitForParallelApprovalActivity` gerektirir.
- [ ] Tek onay akışı stabil çalıştıktan sonra değerlendirilecek.

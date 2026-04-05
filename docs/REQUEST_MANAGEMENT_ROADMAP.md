# Request Management — İlerleme Durumu

## ✅ Tamamlanan Maddeler

### Backend
- [x] ServiceQueue + QueueMembership entity'leri ve strongly-typed ID'ler
- [x] DbContext (2 tablo: `service_queues`, `queue_memberships`)
- [x] CQRS: 5 Command + 2 Query + 7 Handler
- [x] 7 API Endpoint (`/api/req/queues`)
- [x] DTO projeksiyonu (circular JSON reference düzeltmesi)
- [x] IAM'den kullanıcı adı çözümleme (batch user lookup)
- [x] Dinamik form altyapısı (FormSchemaJson, SchemaForm, FormSchemaBuilder)

### Frontend
- [x] `/manage/queues` — master-detail, create modal, departman dropdown, üye yönetimi
- [x] Sidebar → Talep Yönetimi → Hizmet Kuyrukları
- [x] `/dashboard/tickets` — talep listesi sayfası

### Seed Data
- [x] Demo şirket (EntApp Demo) + 2 şube + 6 IAM departman
- [x] 5 demo kullanıcı (org+dept atanmış)
- [x] 4 Request departmanı + 8 hizmet kuyruğu + 13 üyelik + 8 kategori

---

## 📋 Devam Edilecek Maddeler

### 1. Ticket → Queue Routing
- [ ] Kategori seçildiğinde ticket otomatik olarak ilgili kuyruğa yönlendirilsin
- [ ] Kategori ↔ Queue eşlemesi (veya departman üzerinden otomatik)

### 2. Claim/Unclaim (Üzerime Al / Havuza Bırak)
- [ ] Kuyruk üyeleri talepleri "üzerine alsın" (`AssignedUserId` set)
- [ ] Üzerinden bırakabilsin (tekrar havuza düşsün)
- [ ] Backend: `ClaimTicketCommand`, `UnclaimTicketCommand`
- [ ] Frontend: Ticket detayında "Üzerime Al" / "Havuza Bırak" butonları

### 3. Taleplerim Sayfası (Talep Sahibi Görünümü)
- [ ] Kullanıcının kendi oluşturduğu talepleri listesi
- [ ] Durum takibi, detay görüntüleme
- [ ] Gerektiğinde düzenleme imkanı

### 4. Triage / Dispatcher Akışı
- [ ] Dispatcher'ın gelen talepleri sınıflandırması
- [ ] Kategori atama ve ilgili kuyruğa route etme
- [ ] Önceliklendirme

### 5. Workflow Entegrasyonu (Elsa)
- [ ] Kategori bazlı workflow tanımı
- [ ] Talep oluşturulduğunda otomatik workflow başlatma
- [ ] Onay akışları (departman yöneticisi approval)

### 6. Organizasyon Yönetim Sayfası
- [ ] `/manage/organizations` — ağaç görünümü
- [ ] Departman ekleme/düzenleme

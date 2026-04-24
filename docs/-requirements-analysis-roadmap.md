# 16c -- Requirements & Analysis -- Detayli Yol Haritasi

> **Ana roadmap:** [-roadmap.md (Faz 16)]
> **Durum:** MVP tamamlandi

---

## Tasarim Kararlari

> [!IMPORTANT]
> **Hiyerarsik Gereksinim Yapisi:** Requirement entity'si hem ust-seviye fonksiyonel spec hem de
> atomik gereksinim olarak kullanilir. Ust gereksinimin Description alani spec dokumani yerine gecer,
> alt gereksinimler izlenebilir atomik kayitlardir. WorkItem hiyerarsisi (Epic->Story->Task) ile ayni mantik.

> [!NOTE]
> **Ticket -> Requirement Iliskisi:** Her gereksinimin opsiyonel bir SourceTicketId'si vardir.
> Ticket bir gereksinim DEGILDIR -- ticket olgunlastirilarak gereksinim DOGURUYOR.
> Ticket intake form verileri, ``Projeye Aktar`` sirasinda FeatureSpec gereksiniminin Description'ina donusturulur.

> [!NOTE]
> **Numaralama:** Gereksinimler proje bazli KEY-R1, KEY-R2 seklinde numaralanir.
> Ticket'lar REQ-0001 kullanir -- cakisma yok.

---

## MVP Kapsami

### Requirement Entity (TaskManagement modulu icinde)
- [x] ``Requirement`` entity: ProjectId, Key (KEY-RN), Title, Description (Markdown), AcceptanceCriteria
- [x] ``RequirementId`` strongly-typed ID
- [x] ``RequirementType`` enum: FeatureSpec, Functional, NonFunctional, Interface, Constraint
- [x] ``RequirementPriority`` enum (MoSCoW): Must, Should, Could, WontHave
- [x] ``RequirementStatus`` enum: Draft, InReview, Approved, Implemented, Verified
- [x] ``ParentRequirementId`` (nullable FK) -- hiyerarsik yapi (FeatureSpec -> atomik gereksinimler)
- [x] ``SourceTicketId`` + ``SourceTicketNumber`` -- ticket baglantisi (nullable)
- [x] ``ExternalDesignUrl`` -- Figma/Miro link (nullable)
- [x] Proje bazli sequential numaralama (KEY-R1, KEY-R2...)

### WorkItem Baglantisi
- [x] ``WorkItemBase.RequirementId`` (nullable FK) -- is kalemi hangi gereksinimden doguyor
- [x] Gereksinim detay sayfasinda bagli work item'lari gosterme
- [ ] Coverage tracking: ust gereksinimin altindaki tum atomik gereksinimler implemente edildi mi?

### CRUD API
- [x] ``POST /api/pm/projects/{projectId}/requirements`` -- yeni gereksinim
- [x] ``GET /api/pm/projects/{projectId}/requirements`` -- proje gereksinimleri (flat + tree)
- [x] ``GET /api/pm/requirements/{id}`` -- detay (alt gereksinimler + bagli work item'lar dahil)
- [x] ``PUT /api/pm/requirements/{id}`` -- guncelle
- [x] ``DELETE /api/pm/requirements/{id}`` -- sil

### Frontend UI
- [x] Proje detay sayfasinda ``Gereksinimler`` tab'i
- [x] Gereksinim listesi (hiyerarsik -- ust/alt gorunum)
- [x] Gereksinim olusturma/duzenleme formu (tip, oncelik, durum, aciklama, kabul kriterleri)
- [x] Gereksinim detay paneli (Description, AcceptanceCriteria, alt gereksinimler, bagli work item'lar)
- [x] FeatureSpec gereksiniminin altina atomik gereksinim ekleme

---

## Sonraki Fazlar (MVP Sonrasi)

### Onay Akisi
- [ ] Gereksinim durumlari StateFlow Engine ile yonetme (Draft -> InReview -> Approved)
- [ ] Onay workflow'u (WaitForApprovalActivity ile)
- [ ] RequirementApproval kaydi (kim, ne zaman, karar)

### Traceability (Test Management ile birlikte - 16d)
- [ ] Traceability matrix (Gereksinim -> WorkItem -> Test -> Release)
- [ ] Gereksinim coverage raporu -- hangi gereksinimler implemente edildi, test edildi

### Mockup & Tasarim
- [ ] Mockup versiyonlama + diff gorunumu
- [x] Figma/Balsamiq iframe embed (DesignPreview component -- URL otomatik algilama)
- [x] CSP frame-src: embed.figma.com, balsamiq.cloud
- [ ] Figma REST API ile snapshot alma + MinIO storage
- [ ] Mockup <-> Gereksinim baglantisi

### Figma Token Yonetimi (Faz 2)
- [ ] Tenant bazli Figma API token yonetimi (TenantSettings tablosu)
- [ ] Token sifreleme (DataProtection API)
- [ ] Figma REST API entegrasyonu (dosya listesi, node render)

### Ileri Ozellikler
- [ ] BusinessRule entity -- gereksinime bagli is kurallari
- [ ] AnalysisDocument -- ayri belge yonetimi (veya Knowledge Base/Wiki modulu ile)
- [ ] AI destekli gereksinim onerisi (ticket iceriginden otomatik gereksinim taslagi)

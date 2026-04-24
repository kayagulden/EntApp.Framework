# 16e -- Release Management -- Detayli Yol Haritasi

> **Ana roadmap:** [-roadmap.md (Faz 16)]
> **Bagimliliklar:** 16b (Project & WorkItem), 16d (Test Management), StateFlow Engine
> **Modul Konumu:** TaskManagement modulu icinde (pm schema)
> **Durum:** Henuz baslanmadi

---

## Tasarim Kararlari

> [!IMPORTANT]
> **Modul Konumu:** Release entity'leri TaskManagement modulu icinde yashar.
> Release, WorkItem ve TestPlan ile ayni DbContext'i (pm schema) paylashir.
> FK ilishkileri dogrudan kurulur -- ayri module gerek yok.

> [!NOTE]
> **Numaralama:** Release'ler proje bazli KEY-REL1, KEY-REL2 sheklinde numaralanir.
> (Mevcut pattern: WorkItem = KEY-WI-N, Requirement = KEY-RN, TestScenario = KEY-TCN)

> [!NOTE]
> **Versiyonlama:** Release versiyon numarasi serbest text olarak girilir.
> SemVer (v1.2.3), CalVer (2026.04.1) veya custom format desteklenir.
> Validasyon client-side onerilir, backend zorunlu kilmaz.

> [!IMPORTANT]
> **Lifecycle:** Release durumlari MVP'de enum-based sabit akish kullanilir:
> Planning - CodeFreeze - Testing - GoNoGo - Staging - Deployed - Closed
> Ileride StateFlow Engine ile dinamik akisha gecilebilir (Faz 2).

---

## Faz 1 -- MVP (Release + Go/No-Go + Release Notes)

### Entity: Release
- [ ] Id: ReleaseId (strongly-typed)
- [ ] ProjectId: ProjectId (FK)
- [ ] Key: string (KEY-REL1)
- [ ] Version: string (v1.0.0, 2026.04.1 -- serbest format)
- [ ] Title: string (Release bashligi)
- [ ] Description: string? (Markdown)
- [ ] Status: ReleaseStatus enum
- [ ] Type: ReleaseType enum (Major, Minor, Patch, Hotfix, Rollback)
- [ ] SprintId: SprintId? (FK -- hangi sprint'in ciktisi)
- [ ] MilestoneId: MilestoneId? (FK -- hangi milestone'a bagli)
- [ ] PlannedDate: DateOnly? (planlanan release tarihi)
- [ ] ActualDate: DateOnly? (gerceklesen tarih)
- [ ] CodeFreezeDate: DateOnly? (kod dondurma tarihi)
- [ ] ReleaseManagerId: string? (sorumlu kishi)
- [ ] TargetEnvironment: string? (Production, Staging, vb.)
- [ ] Tags: string? (virgulle ayrilmish etiketler)
- [ ] SortOrder: int
- [ ] TenantId, audit alanlari (AuditableEntity + ITenantEntity)

### Entity: ReleaseItem (Release - WorkItem M:N)
- [ ] Id: Guid
- [ ] ReleaseId: ReleaseId (FK)
- [ ] WorkItemId: WorkItemId (FK)
- [ ] IncludedAt: DateTime (ne zaman eklendi)
- [ ] IncludedBy: string (kim ekledi)
- [ ] Notes: string? (ekleme notu)
- [ ] SortOrder: int

### Entity: GoNoGoChecklist
- [ ] Id: GoNoGoChecklistId (strongly-typed)
- [ ] ReleaseId: ReleaseId (FK -- 1:1 ilishki)
- [ ] Status: GoNoGoStatus enum (Pending, InProgress, Approved, Rejected)
- [ ] DecisionAt: DateTime? (karar tarihi)
- [ ] DecisionBy: string? (karari veren)
- [ ] DecisionNotes: string? (karar notu)
- [ ] TenantId, audit alanlari

### Entity: GoNoGoItem (Checklist maddesi)
- [ ] Id: Guid
- [ ] ChecklistId: GoNoGoChecklistId (FK)
- [ ] Category: GoNoGoCategory enum (Development, QA, Operations, Security, Business, Legal)
- [ ] Title: string (kontrol maddesi bashligi)
- [ ] Description: string? (detay)
- [ ] Status: GoNoGoItemStatus enum (Pending, Approved, Rejected, NotApplicable)
- [ ] ReviewedBy: string? (onaylayan kishi)
- [ ] ReviewedAt: DateTime? (onay tarihi)
- [ ] Notes: string? (yorum)
- [ ] SortOrder: int
- [ ] IsRequired: bool (zorunlu mu? false ise skip edilebilir)

### Entity: ReleaseNote
- [ ] Id: ReleaseNoteId (strongly-typed)
- [ ] ReleaseId: ReleaseId (FK -- 1:1)
- [ ] Content: string (Markdown -- otomatik uretilir, duzenlenebilir)
- [ ] GeneratedAt: DateTime (son uretim tarihi)
- [ ] IsManuallyEdited: bool (kullanici duzenledi mi)
- [ ] PublishedAt: DateTime? (yayinlanma tarihi)
- [ ] TenantId, audit alanlari

### Enum'lar
- [ ] ReleaseStatus: Planning, CodeFreeze, Testing, GoNoGo, Staging, Deployed, Closed, Cancelled, Rollback
- [ ] ReleaseType: Major, Minor, Patch, Hotfix, Rollback
- [ ] GoNoGoStatus: Pending, InProgress, Approved, Rejected
- [ ] GoNoGoCategory: Development, QA, Operations, Security, Business, Legal
- [ ] GoNoGoItemStatus: Pending, Approved, Rejected, NotApplicable

### Sequence & Key
- [ ] ProjectBase'e ReleaseSequence (int) eklenmesi
- [ ] NextReleaseKey() metodu: KEY-REL1, KEY-REL2, ...

### CRUD API

#### Release
- [ ] POST /api/pm/projects/{projectId}/releases -- release olustur
- [ ] GET  /api/pm/projects/{projectId}/releases -- proje release listesi (filtreli)
- [ ] GET  /api/pm/releases/{id} -- release detay (items + checklist + note dahil)
- [ ] PUT  /api/pm/releases/{id} -- release guncelle
- [ ] DELETE /api/pm/releases/{id} -- release sil (soft delete)
- [ ] PUT  /api/pm/releases/{id}/status -- durum gecishi

#### Release Items (WorkItem atama)
- [ ] POST /api/pm/releases/{releaseId}/items -- WorkItem ekle
- [ ] DELETE /api/pm/releases/{releaseId}/items/{workItemId} -- WorkItem kaldir
- [ ] POST /api/pm/releases/{releaseId}/items/from-sprint -- Sprint'teki tum done WorkItem'lari ekle
- [ ] GET  /api/pm/releases/{releaseId}/items -- release'deki WorkItem listesi

#### Go/No-Go
- [ ] POST /api/pm/releases/{releaseId}/go-no-go -- checklist olustur (varsayilan maddelerle)
- [ ] GET  /api/pm/releases/{releaseId}/go-no-go -- checklist detay
- [ ] PUT  /api/pm/releases/{releaseId}/go-no-go/items/{itemId} -- madde onay/ret
- [ ] POST /api/pm/releases/{releaseId}/go-no-go/items -- yeni madde ekle
- [ ] PUT  /api/pm/releases/{releaseId}/go-no-go/decide -- genel karar (Approved/Rejected)

#### Release Note
- [ ] POST /api/pm/releases/{releaseId}/release-note/generate -- otomatik uret
- [ ] GET  /api/pm/releases/{releaseId}/release-note -- notu getir
- [ ] PUT  /api/pm/releases/{releaseId}/release-note -- notu duzenle
- [ ] GET  /api/pm/releases/{releaseId}/release-note/export?format=markdown -- export

### Frontend UI

#### Releases Tab (Proje Detay)
- [ ] Release listesi (versiyon, tip badge, durum badge, tarih, ilerleme)
- [ ] Release olusturma/duzenleme formu
- [ ] Release durum gecishi butonlari (status pipeline gorsel)
- [ ] Sprint'ten otomatik WorkItem ekleme

#### Release Detay Paneli
- [ ] Release bilgileri (versiyon, tarihler, sorumlu)
- [ ] Dahil edilen WorkItem'lar listesi (tip/durum badge)
- [ ] WorkItem ekleme/kaldirma
- [ ] Ilerleme ozeti (Feature/Bug/Task dagilimi)

#### Go/No-Go Checklist UI
- [ ] Kategori bazli gruplanmish checklist
- [ ] Her madde icin Approved/Rejected/NotApplicable butonlari
- [ ] Kategori bazli ilerleme cubugu
- [ ] Genel karar butonu (tum zorunlu maddeler onaylandiginda aktif)

#### Release Note UI
- [ ] Otomatik uretilen Markdown onizleme
- [ ] Markdown editor (duzenleme modu)
- [ ] Export butonu (Markdown download)

### Database
- [ ] pm.releases tablosu
- [ ] pm.release_items tablosu
- [ ] pm.go_no_go_checklists tablosu
- [ ] pm.go_no_go_items tablosu
- [ ] pm.release_notes tablosu
- [ ] pm.projects: ReleaseSequence sayaci

### Release Note Uretim Mantigi
- [ ] Release'deki WorkItem'lari Type'a gore grupla (Feature, Bug, Task, vb.)
- [ ] Her WorkItem icin: "- [KEY-WI-N] Title" formatinda satir
- [ ] Baslik: "# Release Notes -- v{Version} ({Date})"
- [ ] Bolumler: New Features, Bug Fixes, Improvements, Other

---

## Faz 1.5 -- MVP Iyilestirmeler

### Release Akish Iyilestirmeleri
- [ ] Release durum gecishi kural kontrolu (GoNoGo onaylanmadan Deployed'a gecilemez)
- [ ] Otomatik CodeFreeze: belirli tarihte status otomatik degishim
- [ ] Release timeline/Gantt gorunumu
- [ ] Release karshilashtirma (iki release arasindaki farklar)

### Bildirim & Otomasyon
- [ ] Release durum degishikliginde bildirim
- [ ] GoNoGo bekleyen onaylar icin hatirlatma
- [ ] Release takvimi (proje bazli -- planlanan release'ler)

### WorkItem Otomasyonu
- [ ] Sprint tamamlandiginda otomatik release draft olusturma onerisi
- [ ] "Done" durumdaki WorkItem'larin otomatik release'e eklenmesi
- [ ] WorkItem'in hangi release'de yayinlandigini gosteren badge

---

## Faz 2 -- Ileri Ozellikler

### StateFlow Entegrasyonu
- [ ] Release lifecycle StateFlow Engine ile yonetim
- [ ] Dinamik durum ve gecish kurallari (admin tarafindan tanimlanabilir)
- [ ] Durum degishikliginde otomatik aksiyon tetikleme
- [ ] React Flow designer ile release akishi tasarlama

### Test Management Baglantisi
- [ ] Release'e TestPlan baglama (hangi test plani bu release icin calistirildi)
- [ ] Release'in deploy edilebilmesi icin TestPlan sonucu kontrolu (tum testler Pass mi?)
- [ ] Release detayinda test sonuc ozeti (pass/fail/blocked)
- [ ] TestExecution sonuclarinin Go/No-Go QA maddesine otomatik yansimasi

### Environment Tracking
- [ ] Environment entity (Dev, Test, Staging, Production -- proje bazli)
- [ ] Deployment entity (hangi release hangi environment'a ne zaman deploy edildi)
- [ ] Environment bazli release durumu
- [ ] Deployment gecmishi timeline

### Rollback & Hotfix
- [ ] Rollback akishi: Deployed release'i geri alma kaydi
- [ ] Hotfix release: mevcut production release'den fork
- [ ] Rollback nedeni ve etki analizi kaydi
- [ ] Hotfix: cherry-pick WorkItem secimi

### Onay Workflow
- [ ] Go/No-Go onayi icin Approval modulu entegrasyonu (mevcut altyapi)
- [ ] Paralel onay (Dev + QA + Ops ayni anda)
- [ ] Escalation: belirli surede onay gelmezse ust yoneticiye bildirim
- [ ] Onay gecmishi ve audit trail

---

## Faz 3 -- CI/CD & External Entegrasyon

### 3a -- CI/CD Pipeline Entegrasyonu
- [ ] Pipeline entity (Jenkins, Azure DevOps Pipeline, GitHub Actions)
- [ ] Pipeline trigger: Release durumuna gore otomatik pipeline tetikleme
- [ ] Pipeline sonucu: basharili/basharisiz geri bildirim
- [ ] Deployment kaniti (pipeline run URL, commit SHA, artifact URL)

### 3b -- Azure DevOps Release Entegrasyonu
- [ ] Azure DevOps Release Pipeline baglantisi (tenant bazli)
- [ ] Release gate olarak Go/No-Go entegrasyonu
- [ ] Azure DevOps Work Item - EntApp WorkItem eshleshtirme
- [ ] Deployment status senkronizasyonu

### 3c -- GitHub Release Entegrasyonu
- [ ] GitHub Release olusturma (API uzerinden)
- [ ] GitHub tag olusturma
- [ ] Release note'u GitHub Release description'a aktarma
- [ ] GitHub Actions deployment status takibi

### 3d -- Container & Artifact Registry
- [ ] Docker image tag ile release eshleshtirme
- [ ] Container registry entegrasyonu (ACR, Docker Hub, GitLab)
- [ ] Artifact versiyonlama (NuGet, npm, PyPI paket eshleshtirme)

### 3e -- Changelog & Notification
- [ ] Changelog otomatik uretimi (tum release'lerden kumulatif)
- [ ] CHANGELOG.md dosyasi uretimi (Keep a Changelog formati)
- [ ] Slack/Teams webhook ile release bildirimi
- [ ] E-posta ile stakeholder bilgilendirmesi

---

## Faz 4 -- Ileri Raporlama & Analytics

### Release Metrikleri
- [ ] Release frekansi (aylik/haftalik release sayisi)
- [ ] Lead time (WorkItem olusturma - release'e dahil olma suresi)
- [ ] Deployment frekansi (DORA metrigi)
- [ ] Change failure rate (rollback/hotfix orani -- DORA metrigi)
- [ ] Mean time to recovery (MTTR -- hatadan kurtulma suresi)
- [ ] Release bazli bug orani (release sonrasi bildirilen hata sayisi)

### Dashboard Widget'lari
- [ ] Release takvimi (Gantt/timeline)
- [ ] Aktif release pipeline durumu
- [ ] Son 10 release -- bashari/rollback orani
- [ ] DORA metrikleri dashboard'u

### Karshilashtirmali Analiz
- [ ] Release boyutu trendi (WorkItem sayisi zaman serisi)
- [ ] Hotfix vs planli release orani
- [ ] Deployment environment dagilimi

---

## Ilishki Haritasi

```
Portfolio
  +-- Project
        +-- Sprint ----------------------------+
        +-- Milestone ----------------------+  |
        +-- WorkItem (Epic/Feature/Bug)     |  |
        |     +-- Requirement               |  |
        |     +-- TestScenario              |  |
        +-- TestPlan --------------------+  |  |
        |     +-- TestExecution          |  |  |
        +-- Release <--------------------+--+--+
              +-- ReleaseItem (-> WorkItem) M:N
              +-- GoNoGoChecklist
              |     +-- GoNoGoItem
              +-- ReleaseNote
              +-- (Faz 2) TestPlan baglantisi
              +-- (Faz 2) Deployment (-> Environment)
```

---

## Dogrulama Plani

### Build ve API
- [ ] dotnet build -- 0 hata
- [ ] Release CRUD API testi
- [ ] WorkItem atama + Sprint'ten toplu ekleme testi
- [ ] Go/No-Go checklist olusturma, onay, karar testi
- [ ] Release note uretim ve export testi
- [ ] Durum gecishi kural kontrolu testi

### Frontend
- [ ] Releases tab -- liste, form, durum gecishi
- [ ] Release detay -- WorkItem listesi, ekleme/kaldirma
- [ ] Go/No-Go checklist UI -- onay akishi
- [ ] Release note -- uretim, duzenleme, export
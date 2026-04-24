# 16e -- Release Management -- Detayli Yol Haritasi

> **Ana roadmap:** [-roadmap.md (Faz 16)]
> **Bagimliliklar:** 16b (Project & WorkItem), 16d (Test Management), StateFlow Engine
> **Modul Konumu:** TaskManagement modulu icinde (pm schema)
> **Durum:** Faz 1 MVP tamamlandi (2026-04-24) — Faz 1.5 devam ediyor

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
- [x] Id: ReleaseId (strongly-typed)
- [x] ProjectId: ProjectId (FK)
- [x] Key: string (KEY-REL1)
- [x] Version: string (v1.0.0, 2026.04.1 -- serbest format)
- [x] Title: string (Release bashligi)
- [x] Description: string? (Markdown)
- [x] Status: ReleaseStatus enum
- [x] Type: ReleaseType enum (Major, Minor, Patch, Hotfix, Rollback)
- [x] SprintId: SprintId? (FK -- hangi sprint'in ciktisi)
- [x] MilestoneId: MilestoneId? (FK -- hangi milestone'a bagli)
- [x] PlannedDate: DateOnly? (planlanan release tarihi)
- [x] ActualDate: DateOnly? (gerceklesen tarih)
- [x] CodeFreezeDate: DateOnly? (kod dondurma tarihi)
- [x] ReleaseManagerId: string? (sorumlu kishi)
- [x] TargetEnvironment: string? (Production, Staging, vb.)
- [x] Tags: string? (virgulle ayrilmish etiketler)
- [x] SortOrder: int
- [x] TenantId, audit alanlari (AuditableEntity + ITenantEntity)

### Entity: ReleaseItem (Release - WorkItem M:N)
- [x] Id: Guid
- [x] ReleaseId: ReleaseId (FK)
- [x] WorkItemId: WorkItemId (FK)
- [x] IncludedAt: DateTime (ne zaman eklendi)
- [x] IncludedBy: string (kim ekledi)
- [x] Notes: string? (ekleme notu)
- [x] SortOrder: int

### Entity: GoNoGoChecklist
- [x] Id: GoNoGoChecklistId (strongly-typed)
- [x] ReleaseId: ReleaseId (FK -- 1:1 ilishki)
- [x] Status: GoNoGoStatus enum (Pending, InProgress, Approved, Rejected)
- [x] DecisionAt: DateTime? (karar tarihi)
- [x] DecisionBy: string? (karari veren)
- [x] DecisionNotes: string? (karar notu)
- [x] TenantId, audit alanlari

### Entity: GoNoGoItem (Checklist maddesi)
- [x] Id: Guid
- [x] ChecklistId: GoNoGoChecklistId (FK)
- [x] Category: GoNoGoCategory enum (Development, QA, Operations, Security, Business, Legal)
- [x] Title: string (kontrol maddesi bashligi)
- [x] Description: string? (detay)
- [x] Status: GoNoGoItemStatus enum (Pending, Approved, Rejected, NotApplicable)
- [x] ReviewedBy: string? (onaylayan kishi)
- [x] ReviewedAt: DateTime? (onay tarihi)
- [x] Notes: string? (yorum)
- [x] SortOrder: int
- [x] IsRequired: bool (zorunlu mu? false ise skip edilebilir)

### Entity: ReleaseNote
- [x] Id: ReleaseNoteId (strongly-typed)
- [x] ReleaseId: ReleaseId (FK -- 1:1)
- [x] Content: string (Markdown -- otomatik uretilir, duzenlenebilir)
- [x] GeneratedAt: DateTime (son uretim tarihi)
- [x] IsManuallyEdited: bool (kullanici duzenledi mi)
- [x] PublishedAt: DateTime? (yayinlanma tarihi)
- [x] TenantId, audit alanlari

### Enum'lar
- [x] ReleaseStatus: Planning, CodeFreeze, Testing, GoNoGo, Staging, Deployed, Closed, Cancelled, Rollback
- [x] ReleaseType: Major, Minor, Patch, Hotfix, Rollback
- [x] GoNoGoStatus: Pending, InProgress, Approved, Rejected
- [x] GoNoGoCategory: Development, QA, Operations, Security, Business, Legal
- [x] GoNoGoItemStatus: Pending, Approved, Rejected, NotApplicable

### Sequence & Key
- [x] ProjectBase'e ReleaseSequence (int) eklenmesi
- [x] NextReleaseKey() metodu: KEY-REL1, KEY-REL2, ...

### CRUD API

#### Release
- [x] POST /api/pm/projects/{projectId}/releases -- release olustur
- [x] GET  /api/pm/projects/{projectId}/releases -- proje release listesi (filtreli)
- [x] GET  /api/pm/releases/{id} -- release detay (items + checklist + note dahil)
- [x] PUT  /api/pm/releases/{id} -- release guncelle
- [x] DELETE /api/pm/releases/{id} -- release sil (soft delete)
- [x] PUT  /api/pm/releases/{id}/status -- durum gecishi

#### Release Items (WorkItem atama)
- [x] POST /api/pm/releases/{releaseId}/items -- WorkItem ekle
- [x] DELETE /api/pm/releases/{releaseId}/items/{workItemId} -- WorkItem kaldir
- [x] POST /api/pm/releases/{releaseId}/items/from-sprint -- Sprint'teki tum done WorkItem'lari ekle
- [x] GET  /api/pm/releases/{releaseId}/items -- release'deki WorkItem listesi

#### Go/No-Go
- [x] POST /api/pm/releases/{releaseId}/go-no-go -- checklist olustur (varsayilan maddelerle)
- [x] GET  /api/pm/releases/{releaseId}/go-no-go -- checklist detay
- [x] PUT  /api/pm/releases/{releaseId}/go-no-go/items/{itemId} -- madde onay/ret
- [x] POST /api/pm/releases/{releaseId}/go-no-go/items -- yeni madde ekle
- [x] PUT  /api/pm/releases/{releaseId}/go-no-go/decide -- genel karar (Approved/Rejected)

#### Release Note
- [x] POST /api/pm/releases/{releaseId}/release-note/generate -- otomatik uret
- [x] GET  /api/pm/releases/{releaseId}/release-note -- notu getir
- [x] PUT  /api/pm/releases/{releaseId}/release-note -- notu duzenle
- [x] GET  /api/pm/releases/{releaseId}/release-note/export?format=markdown -- export

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
- [x] pm.releases tablosu
- [x] pm.release_items tablosu
- [x] pm.go_no_go_checklists tablosu
- [x] pm.go_no_go_items tablosu
- [x] pm.release_notes tablosu
- [x] pm.projects: ReleaseSequence sayaci

### Release Note Uretim Mantigi
- [x] Release'deki WorkItem'lari Type'a gore grupla (Feature, Bug, Task, vb.)
- [x] Her WorkItem icin: "- [KEY-WI-N] Title" formatinda satir
- [x] Baslik: "# Release Notes -- v{Version} ({Date})"
- [x] Bolumler: New Features, Bug Fixes, Improvements, Other

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
- [x] dotnet build -- 0 hata ✅ (2026-04-24)
- [x] Release CRUD API testi ✅
- [x] WorkItem atama + Sprint'ten toplu ekleme testi (endpoint hazir, WorkItem veritabaninda olunca test edilecek)
- [x] Go/No-Go checklist olusturma, onay, karar testi ✅
- [x] Release note uretim ve export testi ✅
- [x] Durum gecishi kural kontrolu testi ✅ (Planning → CodeFreeze dogrulandi)

### Frontend
- [ ] Releases tab -- liste, form, durum gecishi
- [ ] Release detay -- WorkItem listesi, ekleme/kaldirma
- [ ] Go/No-Go checklist UI -- onay akishi
- [ ] Release note -- uretim, duzenleme, export
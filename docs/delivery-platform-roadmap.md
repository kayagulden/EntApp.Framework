# Unified Delivery Platform — Geliştirme Yol Haritası

> **Tarih:** 2026-03-28  
> **Temel:** EntApp.Framework (Technical Framework + Core Modüller)  
> **Modül detayları:** [delivery-platform-modules.md](file:///C:/Users/kaya/.gemini/antigravity/brain/b44af52e-bfc8-46ce-a6eb-44bfbbc2913a/delivery-platform-modules.md)  
> **Ön koşul:** EntApp.Framework `_roadmap.md` **Faz 1–10** tamamlanmış olmalı, ardından bu roadmap başlar.

---

## Faz 1 — Proje Kurulumu & Framework Entegrasyonu

- [ ] EntApp.Framework clone/fork → yeni solution oluştur
- [ ] Business Framework katmanını kaldır (CRM, Sales, HR, Finance entity'leri)
- [ ] Docker Compose: PostgreSQL, Redis, Kafka, Keycloak, Seq, Jaeger
- [ ] Framework core modüllerini aktifleştir: IAM, Audit, Configuration, Notification, FileManagement, MultiTenancy, Localization, Workflow
- [ ] Keycloak realm: roller (Admin, PM, BA, Dev, QA, Requester)
- [ ] Projeye özel `appsettings.json`
- [ ] Doğrulama: boş API ayağa kalkar, login çalışır, health check OK

**Çıktı:** Çalışan boş proje, core modüller aktif.

---

## Faz 2 — Frontend Foundation

- [ ] Sidebar menu yapısı (Talepler, Projeler, Analiz, Test, Release, Raporlar)
- [ ] Dashboard shell (placeholder widget'lar)
- [ ] Talep Sahibi Portalı ayrı layout (basit, public-facing)
- [ ] Ortak bileşenler: status badge, priority badge, user avatar, comment thread, file attachment
- [ ] Bildirim çanı (SignalR entegrasyonu)

**Çıktı:** Navigasyon çalışır, boş sayfalar mevcut, portal layout hazır.

---

## Faz 3 — Request Management (Talep Yönetimi)

### 3a — Temel Entity'ler & Konfigürasyon
- [ ] Department entity (hiyerarşik)
- [ ] RequestCategory entity (Name, Code, Department, ElsaWorkflowDefinitionId, FormSchema, SlaDefinitionId, AutoProjectThreshold)
- [ ] Ticket entity (Number, Title, Priority, Status, Channel → RequestCategory bağlantılı)
- [ ] TicketComment entity (IsInternal), TicketStatusHistory entity
- [ ] Ticket CRUD API + durum geçişleri Elsa workflow'dan gelir
- [ ] Frontend: departman + kategori seçimi → dinamik form, ticket listesi, detay, yorum thread

### 3b — Elsa Workflow Entegrasyonu
- [ ] TicketCreated event → Elsa workflow başlatma (kategori bazlı)
- [ ] Custom Elsa aktiviteleri: SetStatus, AssignTo, SendNotification, ApprovalGate, Timer, CreateProject, Condition
- [ ] Her RequestCategory için varsayılan Elsa akışları tanımlama (basit ve karmaşık)
- [ ] Admin panelden Elsa designer ile akış tasarlama/düzenleme
- [ ] Projeye dönüşme: efor eşiği veya kategori bazlı otomatik proje oluşturma

### 3c — SLA Engine
- [ ] SlaDefinition entity (Priority → ResponseTime, ResolutionTime, kategori bazlı)
- [ ] SLA countdown başlatma (TicketCreated event)
- [ ] SLA breach tespiti (Hangfire) → eskalasyon (Notification)
- [ ] Frontend: SLA badge (yeşil/sarı/kırmızı), kalan süre göstergesi

### 3d — Talep Sahibi Portalı
- [ ] Portal: departman + kategori seç → dinamik form (RequestCategory.FormSchema)
- [ ] Portal: taleplerimin listesi + Elsa'dan gelen güncel adım gösterimi
- [ ] Portal: yorum ekleme, dosya yükleme
- [ ] Bildirimler (e-posta + in-app + push)

**Çıktı:** Departman/kategori bazlı konfigüre edilebilir Elsa akışları, SLA, portal, projeye dönüşme çalışır.

---

## Faz 4 — Project & Portfolio Management (Temel)

### 4a — Program & Proje
- [ ] Program entity (programlar → projeler)
- [ ] Project entity (Methodology: Scrum/Kanban/ScrumBan)
- [ ] TeamMember entity (rol, kapasite, uygunluk)
- [ ] Program CRUD, proje CRUD
- [ ] Frontend: program listesi, proje listesi, proje detay (özet dashboard)

### 4b — Backlog
- [ ] BacklogItem entity (UserStory/Task/Bug/Epic/TechDebt)
- [ ] Backlog hiyerarşisi (Epic → Story → Task)
- [ ] Bağımlılık tanımlama (DependsOn)
- [ ] Efor tahmini (StoryPoints, EstimatedHours, T-Shirt size)
- [ ] Backlog CRUD + durum geçişleri
- [ ] Frontend: backlog listesi (tablo + filtre), backlog item detay formu, hiyerarşi görünümü

### 4c — Board
- [ ] BoardColumn entity (Name, Order, WipLimit, proje bazlı)
- [ ] Varsayılan kolonlar oluşturma (New, Active, InReview, Testing, Done)
- [ ] Board API (kolon CRUD, item taşıma)
- [ ] Frontend: Kanban board (drag & drop, WIP limit göstergesi, filtre)
- [ ] WIP limit aşımında kolon kırmızı uyarı

**Çıktı:** Program → Proje → Backlog → Board çalışır, drag & drop aktif.

---

## Faz 5 — Sprint & Kanban Desteği

### 5a — Sprint (Scrum/ScrumBan)
- [ ] Sprint entity (Name, Goal, StartDate, EndDate, Status, Capacity)
- [ ] Sprint planlama: backlog'dan sprint'e item çekme
- [ ] Sprint board: sprint filtresi, velocity göstergesi
- [ ] SprintRetrospective entity
- [ ] Sprint tamamlama: tamamlanmayan item'lar sonraki sprint'e taşıma
- [ ] Frontend: sprint planlama ekranı, sprint board, retrospective formu

### 5b — Kanban Metrikleri
- [ ] Lead time hesaplama (oluşturulma → Done)
- [ ] Cycle time hesaplama (Active → Done)
- [ ] Throughput hesaplama (haftalık tamamlanan)
- [ ] WIP yaşlanma (kaç gündür aynı kolonda)
- [ ] Frontend: Cumulative Flow Diagram bileşeni

### 5c — Metodoloji Bazlı UI
- [ ] Methodology=Scrum → Sprint paneli görünür, velocity metrikleri
- [ ] Methodology=Kanban → Sprint gizli, WIP zorunlu, lead/cycle time metrikleri
- [ ] Methodology=ScrumBan → her ikisi de aktif

**Çıktı:** Scrum sprint akışı ve Kanban sürekli akış aynı board üzerinde çalışır.

---

## Faz 6 — Requirements & Analysis

### 6a — Gereksinim Yönetimi
- [ ] Requirement entity (FR/NFR, talep ve proje bağlantılı)
- [ ] AcceptanceCriteria alanı
- [ ] Gereksinim durum geçişleri (Draft → InReview → Approved → Implemented → Verified)
- [ ] RequirementApproval entity (çok aşamalı onay — Workflow entegrasyonu)
- [ ] Onay sonrası → otomatik BacklogItem oluşturma önerisi
- [ ] Frontend: gereksinim listesi, detay formu, onay akışı takibi

### 6b — İş Kuralları
- [ ] BusinessRule entity (kural numarası, koşul, aksiyon, durum)
- [ ] İş kuralı onay akışı
- [ ] Gereksinime bağlama (opsiyonel)
- [ ] Frontend: iş kuralı listesi, onay

### 6c — Mockup & Analiz Dokümanları
- [ ] Mockup entity (dosya/link, Figma/Miro URL)
- [ ] MockupVersion entity (versiyon takibi)
- [ ] AnalysisDocument entity (ProcessFlow, DataModel, UseCase, rich text)
- [ ] FileManagement modülü entegrasyonu (dosya yükleme)
- [ ] Frontend: mockup galerisi (thumbnail + versiyon), doküman editörü

### 6d — Traceability
- [ ] Gereksinim → BacklogItem bağlantısı (1:N)
- [ ] Gereksinim → TestScenario bağlantısı (1:N)
- [ ] Traceability Matrix API (gereksinim başına: iş kuralı, mockup, backlog, test, release)
- [ ] Frontend: traceability matrix tablosu, gereksinim detayında bağlı item'lar

**Çıktı:** Gereksinim → iş kuralı → mockup → backlog tam zincir, onay akışları çalışır.

---

## Faz 7 — Test Management

### 7a — Test Senaryoları
- [ ] TestScenario entity (Number, Title, Preconditions, Type)
- [ ] TestStep entity (StepOrder, Action, ExpectedResult)
- [ ] Senaryo → Requirement bağlantısı, Senaryo → BacklogItem bağlantısı
- [ ] Frontend: senaryo yazma (adım ekleme/sıralama), senaryo listesi

### 7b — Test Planı & Execution
- [ ] TestPlan entity (Name, PlannedDate, Environment, Status)
- [ ] TestPlanScenario entity (senaryo seçimi, tester atama)
- [ ] TestExecution entity (Pass/Fail/Blocked/Skipped, ActualResult, ekler)
- [ ] Fail → otomatik Bug (BacklogItem, Type=Bug) oluşturma
- [ ] Bug → senaryo bağlantısı (re-test için)
- [ ] Frontend: test planı oluşturma (senaryo seç), execution grid (pass/fail toggle), bug oluşturma dialogu

### 7c — Coverage & Raporlar
- [ ] Test coverage hesaplama: gereksinim başına senaryo sayısı
- [ ] Pass rate: plan/sprint/release bazlı
- [ ] Frontend: coverage raporu, pass rate grafiği

**Çıktı:** Senaryo yazımı → plan oluşturma → execution → bug → re-test tam döngü çalışır.

---

## Faz 8 — Release Management

- [ ] Release entity (Version, PlannedDate, Status, ReleaseType)
- [ ] ReleaseItem entity (BacklogItem bağlantısı, Included/Excluded/Deferred)
- [ ] GoNoGoChecklist + GoNoGoItem entity'leri (kategori bazlı kontrol)
- [ ] Go/No-Go kararı (tüm item'lar Ready → Go)
- [ ] ReleaseNote entity — backlog item'lardan otomatik üretim (AI destekli)
- [ ] Release durumu geçişleri (Planning → Development → CodeFreeze → Testing → Go/No-Go → Deployed → Released)
- [ ] Frontend: release detay (scope listesi, go/no-go checklist, release note editörü)
- [ ] `ReleaseDeployedEvent` → tüm ilgili talep sahiplerine bildirim

**Çıktı:** Release tanım → scope belirleme → go/no-go → release note → deploy bildirimi.

---

## Faz 9 — Developer Tools & IDE Entegrasyonu

### 9a — Git Webhook & Commit Linking
- [ ] Repository entity (Name, Url, Provider, WebhookSecret)
- [ ] CommitLink entity (CommitHash, Message, Author, Branch → BacklogItem)
- [ ] Webhook endpoint: `POST /api/webhooks/git` (GitHub, Azure DevOps, GitLab)
- [ ] Commit mesajından `[WI-XXX]` parse → BacklogItem'a bağla
- [ ] Opsiyonel: ilk commit → item durumunu otomatik InProgress'e çek
- [ ] Frontend: backlog item detayında "Bağlı Commitler" sekmesi

### 9b — Git Hook & Branch Convention
- [ ] `.githooks/commit-msg` hook şablonu (WI-XXX zorunlu)
- [ ] Branch naming convention: `feature/WI-456-login-page`
- [ ] Hook kurulum script'i (proje onboarding)

### 9c — VS Code Extension
- [ ] Extension projesi scaffold (TypeScript, VS Code Extension API)
- [ ] Platform API auth (token bazlı)
- [ ] Task Panel: sidebar'da atanmış item listesi
- [ ] Active Task: status bar'da aktif item göstergesi
- [ ] Auto Prefix: commit mesajına otomatik `[WI-XXX]` ekleme
- [ ] Branch oluşturma: item'dan `feature/WI-XXX-title` otomatik
- [ ] Durum güncelleme: extension içinden InProgress/InReview geçişi
- [ ] Süre kayıt: start/stop timer → item'a actual hours ekleme

### 9d — CLI Aracı
- [ ] `wi list` — bana atanmış item listesi
- [ ] `wi start WI-XXX` — branch oluştur + durum güncelle
- [ ] `wi commit "mesaj"` — otomatik prefix ile commit
- [ ] `wi review WI-XXX` — durumu InReview'a çek
- [ ] `wi log WI-XXX 2h` — süre kaydet
- [ ] NPM veya dotnet tool olarak dağıtım

**Çıktı:** Commit → backlog item bağlantısı, VS Code'da task yönetimi, CLI ile hızlı işlemler.

---

## Faz 10 — Knowledge Base & Change Request

### 10a — Knowledge Base / Wiki
- [ ] WikiSpace, WikiPage, WikiPageVersion entity'leri
- [ ] Sayfa hiyerarşisi (parent-child)
- [ ] Rich text editör (Markdown + WYSIWYG)
- [ ] Versiyon geçmişi ve diff görünümü
- [ ] Full-text arama (AI semantic search entegrasyonu opsiyonel)
- [ ] Sayfa şablonları (ADR, meeting notes, süreç rehberi)
- [ ] Frontend: wiki tree navigation, sayfa editörü, versiyon karşılaştırma

### 10b — Change Request
- [ ] ChangeRequest entity (Number, Title, Reason, ImpactAnalysis, Status)
- [ ] ChangeRequestApproval entity (çok aşamalı onay — Workflow)
- [ ] CR onaylandığında → Requirement yeni versiyon + etkilenen backlog/test işaretleme
- [ ] Scheduling Engine yeniden hesaplama tetikleme
- [ ] Frontend: CR listesi, etki analizi formu, onay akışı

**Çıktı:** Wiki ile dokümantasyon, CR ile kontrollü değişiklik yönetimi çalışır.

---

## Faz 11 — Time Tracking & Risk Management

### 11a — Time Tracking
- [ ] TimeEntry entity (Date, Hours, Description, EntryType)
- [ ] Web UI: backlog item detayında "Süre Ekle" butonu
- [ ] VS Code extension: start/stop timer entegrasyonu
- [ ] CLI: `wi log WI-XXX 3h` komutu
- [ ] Raporlar: kişi bazlı zaman çizelgesi, tahmini vs gerçekleşen sapma

### 11b — Risk Yönetimi
- [ ] Risk entity (Title, Probability, Impact, RiskScore, MitigationPlan, Owner, Status)
- [ ] Risk matrisi hesaplama (Probability × Impact)
- [ ] Frontend: proje detayında riskler sekmesi, ısı haritası görünümü
- [ ] PM dashboard'da açık risk sayısı ve top 3 kritik risk

**Çıktı:** Süre kaydı tüm kanallardan yapılabilir, risk takibi canlı.

---

## Faz 12 — Automation Rules & Cross-Project

### 12a — Automation Rules
- [ ] AutomationRule entity (Name, Trigger, Condition, Action, IsActive)
- [ ] Rule engine: event → condition evaluate → action execute
- [ ] Hazır kurallar: child'lar Done → parent Done, kritik bug → freeze, tahmin aşımı → uyarı
- [ ] Çalışma geçmişi (audit log)
- [ ] Frontend: kural tanımlama wizard, aktif/pasif toggle, geçmiş

### 12b — Cross-Project Dependency
- [ ] BacklogItem.DependsOn → projeler arası bağımlılık desteği
- [ ] Scheduling Engine: multi-project dependency hesaplama
- [ ] Frontend: cross-project dependency haritası (graph görünümü)

### 12c — PWA
- [ ] next.config.js: PWA manifest, service worker
- [ ] Mobil bildirim + onay (push notification → approve/reject)
- [ ] Offline cache (son görüntülenen item'lar)
- [ ] Install prompt

**Çıktı:** Otomasyon kuralları, projeler arası bağımlılık, mobil PWA çalışır.

---

## Faz 13 — Scheduling Engine (Otomatik Takvim)

### 13a — Altyapı
- [ ] WSJF hesaplama (Business Value + Time Criticality + Risk Reduction / SP)
- [ ] Kapasite modeli (TeamMember.Capacity, uygunluk takvimi, tatiller)
- [ ] Bağımlılık grafiği (BacklogItem.DependsOn → DAG, cross-project dahil)

### 13b — Auto-Scheduling Algoritması
- [ ] Scrum modu: backlog → sprint'lere dağıtma (kapasite bazlı)
- [ ] Kanban modu: throughput bazlı tahmini tamamlanma tarihi
- [ ] Bağımlılık çözümü: en erken başlangıç tarihi hesaplama
- [ ] Scheduling API: `POST /api/schedule/calculate`

### 13c — Re-scheduling & Bildirim
- [ ] Periyodik hesaplama: Hangfire job (günlük veya haftalık, PM konfigüre eder)
- [ ] Manuel hesaplama: "Takvimi Yeniden Hesapla" butonu
- [ ] Önceki hesaplama ile karşılaştırma → kayma tespiti
- [ ] Etkilenen item'ların talep sahiplerine bildirim
- [ ] Frontend: Gantt/timeline görünümü, değişiklik diff gösterimi

**Çıktı:** Backlog + kapasite + bağımlılık → periyodik/manuel takvim hesaplama, kayma bildirimi.

---

## Faz 14 — Reporting & Analytics

### 14a — Dashboard'lar
- [ ] Yönetici dashboard: program/proje durumu, açık talep, SLA, upcoming release, top riskler
- [ ] PM dashboard: velocity/throughput trend, burndown/cumulative flow, kaynak kullanımı, risk
- [ ] BA dashboard: gereksinim durumu, onay bekleyen, coverage, açık CR
- [ ] QA dashboard: pass rate, bug trend, severity dağılımı
- [ ] Dev dashboard: bana atanmış item'lar, süre kaydı özeti
- [ ] Talep sahibi dashboard: taleplerimin durumu, tahmini teslim

### 14b — SLA Raporları
- [ ] İlk yanıt süresi, çözüm süresi, SLA uyum oranı
- [ ] Departman/tip/öncelik bazlı kırılım
- [ ] Trend grafikleri (aylık karşılaştırma)

### 14c — Proje Raporları
- [ ] Scrum: velocity trend, burndown, sprint goal tutturma
- [ ] Kanban: lead time, cycle time, throughput, cumulative flow diagram
- [ ] Backlog aging, kaynak kullanım oranı
- [ ] Tahmini vs gerçekleşen süre sapma raporu

### 14d — Export
- [ ] Dashboard → PDF export
- [ ] Rapor verileri → Excel export

**Çıktı:** Rol bazlı dashboard'lar, SLA/proje/test/süre raporları canlı.

---

## Faz 15 — AI Özellikleri

- [ ] **Efor Tahmini:** geçmiş item'ların SP/süre verisi → yeni item için AI tahmin önerisi
- [ ] **Test Senaryo Önerisi:** gereksinim metni → AI ile test senaryosu taslağı üretimi
- [ ] **Release Note Üretimi:** backlog item listesi → AI ile release note metni
- [ ] **Talep Sınıflandırma:** ticket metni → otomatik tip/öncelik/departman önerisi
- [ ] **Gereksinim Özetleme:** uzun gereksinim → kısa özet üretimi
- [ ] **Akıllı Arama:** doğal dil ile backlog/gereksinim/ticket/wiki arama (RAG)
- [ ] **Etki Analizi:** CR oluşturulduğunda AI ile etkilenen item'ları tespit önerisi

**Çıktı:** AI ile efor tahmini, senaryo üretimi, release note, akıllı arama çalışır.

---

## Faz 16 — DevOps & Production

- [ ] CI/CD pipeline (Azure DevOps)
- [ ] Kubernetes manifest / Helm chart
- [ ] Prometheus + Grafana monitoring
- [ ] Güvenlik kontrolü (OWASP, rate limit, CORS)
- [ ] Performance test
- [ ] Production deployment

**Çıktı:** Production'a deploy edilebilir, monitör edilen uygulama.

---

## Özet Tablo

| Faz | Başlık | Tahmini Süre |
|-----|--------|-------------|
| 1 | Proje Kurulumu & Framework | 2-3 gün |
| 2 | Frontend Foundation | 3-4 gün |
| 3 | Request Management | 2-3 hafta |
| 4 | Project & Portfolio (temel) | 2-3 hafta |
| 5 | Sprint & Kanban Desteği | 1-2 hafta |
| 6 | Requirements & Analysis | 2-3 hafta |
| 7 | Test Management | 2-3 hafta |
| 8 | Release Management | 1-2 hafta |
| 9 | Developer Tools & IDE | 2-3 hafta |
| 10 | Knowledge Base & Change Request | 2-3 hafta |
| 11 | Time Tracking & Risk Management | 1-2 hafta |
| 12 | Automation Rules & Cross-Project & PWA | 2-3 hafta |
| 13 | Scheduling Engine | 2-3 hafta |
| 14 | Reporting & Analytics | 2-3 hafta |
| 15 | AI Özellikleri | 1-2 hafta |
| 16 | DevOps & Production | 1-2 hafta |
| | **Toplam** | **~25-35 hafta** |

> [!NOTE]
> **Ön koşul:** EntApp.Framework `_roadmap.md` **Faz 1–10** tamamlanmış olmalı. Bu takvim framework hazır olduğu varsayımıyla hesaplanmıştır.


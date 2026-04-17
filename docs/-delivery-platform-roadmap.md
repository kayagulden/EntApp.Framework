# Delivery Platform — ALM/ITSM Modülleri Yol Haritası

> **Ana roadmap:** [_roadmap.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/_roadmap.md)
> **Kaynak:** [delivery-platform-modules.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/delivery-platform-modules.md)
> **Detaylı talep yönetimi:** [REQUEST_MANAGEMENT_ROADMAP.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/REQUEST_MANAGEMENT_ROADMAP.md)

> [!IMPORTANT]
> **Ön Koşullar (Faz 10 tamamlanmalı):**
> 1. ✅ `ApprovalCompletedIntegrationEvent` — Faz 16 modülleri bu event'i dinleyerek onay sonrası aksiyonları tetikler
> 2. ✅ Frontend: Bekleyen onaylar listesi — tüm Faz 16 onay akışları bu generic UI'ı kullanır
> 3. `Dinamik form desteği` — 16a (Request Mgmt) başlamadan önce Workflow stepDefinitionsJson + Dynamic UI form schema entegrasyonu yapılmalı
>
> **Mimari ilke:** Faz 16 modülleri Workflow modülünü **loosely coupled** olarak tüketir:
> - Event tanımları → `Workflow.Application/IntegrationEvents/`
> - Event handler'ları → her Faz 16 modülünün kendi `Infrastructure/Handlers/` klasöründe
> - Workflow'u başlatma → `IWorkflowEngine.StartAsync()` ile (ISender üzerinden)

---

## 16a — Request Management (Talep Yönetimi)

> **Backend tamamlanma:** 2026-04-05
> **Workflow entegrasyonu:** 2026-04-17

- [x] `Department`, `RequestCategory`, `SlaDefinition`, `Ticket`, `TicketComment`, `TicketStatusHistory` entity'leri
- [x] Talep oluşturma + SLA takibi (`SlaCalculator`, otomatik deadline hesaplama)
- [x] Sequential ticket numarası (`TicketNumberGenerator` — REQ-0001 formatı)
- [x] CQRS: 12 command, 8 query, 5 validator, 20 handler, 20 API endpoint (`/api/req/`)
- [x] Integration Events: `TicketCreatedEvent`, `TicketAssignedEvent`, `TicketSlaBreachedEvent`, `TicketResolvedEvent`
- [x] `RequestManagementDbContext` (schema: `req`, 6 tablo)
- [x] ServiceQueue + QueueMembership — kuyruk yönetimi, üyelik CRUDs
- [x] Ticket → Queue routing (kategori bazlı DefaultQueueId + RouteToQueue activity)
- [x] Claim (Üzerime Al) — `ClaimTicketCommand`, kuyruk üyelik validasyonu, idempotent
- [x] Elsa v3 workflow entegrasyonu — kategori bazlı otomatik başlatma
- [x] Custom blocking activities: `WaitForAssignment`, `WaitForStatusDecision`, `WaitForAllTasksDone`
- [x] Ticket detay sayfasında dinamik aksiyonlar paneli (bookmark-driven)
- [x] Elsa Designer entegrasyonu (Blazor WASM iframe, programatik seed)
- [ ] Departman/kategori bazlı dinamik form (RequestCategory.FormSchema → Dynamic UI)
- [ ] `CreateTaskForAssignee` activity ile workflow'dan otomatik görev oluşturma (activity mevcut, workflow akışına eklenmedi)
- [ ] Unclaim (Havuza Bırak) — `UnclaimTicketCommand` + frontend butonu
- [ ] Talep sahibi portalı (self-service UI)

---

## 16b — Project & Portfolio Management
- [ ] `Program`, `Project` (genişletilmiş), `BacklogItem`, `Sprint`, `SprintRetrospective`, `BoardColumn`, `TeamMember` entity'leri
- [ ] Metodoloji desteği: Scrum / Kanban / ScrumBan
- [ ] Sprint planlama + burndown + velocity metrikleri
- [ ] Kanban board: drag & drop, WIP limit, swim lanes
- [ ] Backlog hiyerarşisi: Epic → User Story → Task → Sub-task
- [ ] Efor tahmini: Story Points, Saat, T-Shirt, AI tahmini
- [ ] WSJF (Weighted Shortest Job First) önceliklendirme

---

## 16c — Requirements & Analysis
- [ ] `Requirement`, `BusinessRule`, `Mockup`, `MockupVersion`, `AnalysisDocument`, `RequirementApproval` entity'leri
- [ ] Gereksinim durumları + onay akışı (Workflow modülü ile)
- [ ] Traceability matrix (Gereksinim → Backlog → Test → Release)
- [ ] Mockup versiyonlama + Figma/Miro link desteği

---

## 16d — Test Management
- [ ] `TestScenario`, `TestStep`, `TestPlan`, `TestPlanScenario`, `TestExecution` entity'leri
- [ ] Test planı oluşturma + senaryo atama
- [ ] Test execution: Pass/Fail/Blocked + Bug oluşturma (→ BacklogItem)
- [ ] Test coverage raporu (gereksinim, sprint, release bazlı)

---

## 16e — Release Management
- [ ] `Release`, `ReleaseItem`, `GoNoGoChecklist`, `GoNoGoItem`, `ReleaseNote` entity'leri
- [ ] Release akışı: Planning → Code Freeze → Testing → Go/No-Go → Deployed
- [ ] Go/No-Go kontrol listesi (Dev/QA/Ops/Security kategorileri)
- [ ] Release note otomatik üretimi (backlog item'lardan)

---

## 16f — Scheduling Engine
- [ ] Otomatik takvim hesaplama (bağımlılık, kapasite, öncelik)
- [ ] Scrum: Sprint bazlı yerleştirme / Kanban: Sürekli akış tahmini
- [ ] Yeniden hesaplama: periyodik (Hangfire) + manuel
- [ ] Kayma tespit + bildirim

---

## 16g — Knowledge Base / Wiki
- [ ] `WikiSpace`, `WikiPage`, `WikiPageVersion` entity'leri
- [ ] Sayfa hiyerarşisi + rich text editör + versiyon geçmişi
- [ ] Full-text + AI semantic search entegrasyonu

---

## 16h — Ek Modüller
- [ ] Change Request Management (değişiklik talebi + onay + etki analizi)
- [ ] Risk Management (risk matrisi, olasılık × etki)
- [ ] Automation Rules (Trigger → Condition → Action engine)
- [ ] Developer Tools — Git webhook, commit link, VS Code extension (opsiyonel)

---

## 16i — Reporting & Analytics
- [ ] Dashboard'lar: Yönetici, PM, BA, QA, Talep Sahibi
- [ ] SLA raporları (ilk yanıt, çözüm süresi, uyum oranı)
- [ ] Scrum metrikleri (velocity, burndown, sprint hedef tutturma)
- [ ] Kanban metrikleri (lead time, cycle time, throughput, cumulative flow)

---

## 16j — Cross-Module Entegrasyon
- [ ] Modüller arası event haritası (dokümdaki event akışı)
- [ ] Cross-project dependency (projeler arası bağımlılık)
- [ ] Entegrasyon testleri

---

**Çıktı:** Tam özellikli ALM/ITSM platformu — talep yönetimi, proje/portfolio, gereksinim, test, release, wiki, risk, otomasyon.

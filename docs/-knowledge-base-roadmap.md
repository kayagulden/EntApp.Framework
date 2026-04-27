# 16g - Knowledge Base / Wiki - Detayli Yol Haritasi

> **Ana roadmap:** [-roadmap.md (Faz 16)](-roadmap.md)
> **Durum:** Faz A, B, C tamamlandi - Operasyonel + AI Entegrasyonu aktif

---

## Faz A - Core Backend (Tamamlandi)

### Entity'ler
- [x] WikiSpace - wiki alani (proje veya global)
- [x] WikiPage - wiki sayfasi (hiyerarsik, slug, status, lock)
- [x] WikiPageVersion - sayfa versiyon gecmisi

### Sayfa Yonetimi
- [x] Sayfa hiyerarsisi (parent-child tree)
- [x] Versiyon gecmisi + revert
- [x] Sayfa kilitleme (RowVersion concurrent edit korumasi)
- [x] Publish / Archive / Draft yasam dongusu
- [x] Slug bazli erisim (space-slug/page-slug)
- [x] Breadcrumb olusturma

### API Endpoint'leri (20+)
- [x] WikiSpace CRUD (`/api/v1/wiki/spaces`)
- [x] WikiPage CRUD + Tree + Slug (`/api/v1/wiki/pages`)
- [x] Publish, Archive, Lock/Unlock, Move
- [x] Version listesi, detay, revert
- [x] Search (`/api/v1/wiki/search`)

### Altyapi
- [x] `kb` PostgreSQL schema (auto-create)
- [x] KnowledgeBaseDbContext - EF Core
- [x] MediatR CQRS handler'lari
- [x] Tenant izolasyonu (X-Tenant-Id)

---

## Faz B - Frontend (Tamamlandi)

### Bilesenler
- [x] RichTextEditor - Tiptap v3 entegrasyonu (bold, italic, tables, images, headings, task list, code block)
- [x] RichTextViewer - Salt okunur icerik goruntuleme
- [x] WikiSpaceList - Space kartlari listesi
- [x] WikiPageTreeView - Hiyerarsik sayfa agaci

### Sayfalar
- [x] `/dashboard/wiki` - Wiki ana sayfa (space listesi + yeni space olusturma)
- [x] `/dashboard/wiki/[id]` - Space detay (sayfa agaci + editor + breadcrumb)

### Entegrasyon
- [x] Sidebar'a Bilgi Bankasi linki (BookOpen ikonu)
- [x] Proje Dashboard'a Wiki tab'i (WikiTab bileseni)

---

## Faz C - Arama ve AI Entegrasyon (Tamamlandi)

### Full-Text Search
- [x] PostgreSQL tsvector full-text search (to_tsvector turkish)
- [x] ts_rank ile relevance scoring
- [x] Prefix match destegi (kelime:*)
- [x] LIKE fallback (tsvector kullanilamadiginda)
- [x] Self-service deflection endpoint (`/api/v1/wiki/suggest?q=...`)

### Ticket -> KB Entegrasyonu
- [x] TicketResolvedKbHandler - TicketResolvedEvent dinler (MediatR INotificationHandler)
- [x] Destek Bilgi Bankasi space'ine otomatik KB taslagi olusturma
- [x] AI prompt render (IPromptManager) + fallback prompt
- [x] ILlmService ile makale uretimi
- [x] GenerateKbFromTicketCommand - Manuel tetik (`/api/v1/wiki/generate-from-ticket/{id}`)

### Requirement -> Wiki Entegrasyonu
- [x] GenerateWikiFromRequirementCommand - FeatureSpec + child requirement'lardan spec dokumani
- [x] Proje wiki space'i otomatik olusturma
- [x] Mevcut wiki sayfasi varsa yeni versiyon olusturma (idempotent)
- [x] AI destekli yapilandirilmis HTML spec dokumani uretimi
- [x] Endpoint: `/api/v1/wiki/generate-from-requirement/{id}`
- [x] Frontend: RequirementsTab'da FeatureSpec icin Wiki'ye Yayinla butonu

### Semantic Search (Opsiyonel)
- [ ] Docker Compose'a pgvector extension eklenmesi
- [ ] RAG altyapisi ile embedding bazli arama

---

## Kalan Iyilestirmeler (Gelecek)

### Prompt Sablonlari
- [ ] kb-article-from-ticket prompt template DB seed
- [ ] wiki-spec-from-requirement prompt template DB seed

### Frontend Gelistirmeler
- [ ] Ticket olusturma formunda KB oneri paneli (debounced suggest)
- [ ] Wiki sayfa diff gorunumu (versiyon karsilastirma)
- [ ] Collaborative editing indicator (lock status UI)

### Altyapi
- [ ] ICurrentUser entegrasyonu (auth modulu ile)
- [ ] Sayfa etiketleri (tags/labels)
- [ ] Wiki export (PDF/Markdown)

---

## Teknik Dosya Referanslari

| Katman | Dosya |
|--------|-------|
| Domain | src/Modules/KnowledgeBase/.../Domain/Entities/ |
| Application | src/Modules/KnowledgeBase/.../Application/ |
| Infrastructure | src/Modules/KnowledgeBase/.../Infrastructure/ |
| CRUD Handlers | Infrastructure/Handlers/KnowledgeBaseHandlers.cs |
| AI Handlers | Infrastructure/Handlers/KnowledgeBaseIntegrationHandlers.cs |
| Endpoints | Infrastructure/Endpoints/KnowledgeBaseEndpoints.cs |
| DbContext | Infrastructure/Persistence/KnowledgeBaseDbContext.cs |
| Frontend Editor | src/Frontend/entapp-web/src/components/shared/RichTextEditor.tsx |
| Wiki Components | src/Frontend/entapp-web/src/components/wiki/WikiComponents.tsx |
| Wiki Pages | src/Frontend/entapp-web/src/app/dashboard/wiki/ |
| Project WikiTab | src/Frontend/entapp-web/src/app/dashboard/projects/[id]/WikiTab.tsx |
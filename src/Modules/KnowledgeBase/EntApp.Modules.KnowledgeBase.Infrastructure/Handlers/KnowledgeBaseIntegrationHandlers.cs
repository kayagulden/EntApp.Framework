using EntApp.Modules.AI.Application.DTOs;
using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.KnowledgeBase.Application.Commands;
using EntApp.Modules.KnowledgeBase.Application.Queries;
using EntApp.Modules.KnowledgeBase.Domain.Entities;
using EntApp.Modules.KnowledgeBase.Domain.Ids;
using EntApp.Modules.KnowledgeBase.Infrastructure.Persistence;
using EntApp.Modules.RequestManagement.Application.IntegrationEvents;
using EntApp.Modules.TaskManagement.Application.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace EntApp.Modules.KnowledgeBase.Infrastructure.Handlers;

// ══════════════════════════════════════════════════════════════
// FAZ C — ARAMA & AI ENTEGRASYONU
// ══════════════════════════════════════════════════════════════

// ── tsvector Full-Text Search ────────────────────────────────
// Mevcut SearchWikiPagesQueryHandler'daki LIKE aramasını
// PostgreSQL tsvector ile güçlendiren yardımcı sorgu.
// SearchWikiPagesQueryHandler mevcut handler dosyasında tsvector
// desteği eklenmiştir (aşağıda SuggestKb handler'ı aynı tekniği kullanır).

// ── Self-Service Deflection (Suggest) ────────────────────────
public sealed class SuggestKbArticlesQueryHandler(
    KnowledgeBaseDbContext db,
    ILogger<SuggestKbArticlesQueryHandler> logger)
    : IRequestHandler<SuggestKbArticlesQuery, List<WikiPageSearchDto>>
{
    public async Task<List<WikiPageSearchDto>> Handle(SuggestKbArticlesQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SearchText))
            return [];

        // PostgreSQL tsvector full-text search
        var searchTerms = request.SearchText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .Select(t => t + ":*") // prefix match
            .ToArray();

        if (searchTerms.Length == 0) return [];

        var tsQuery = string.Join(" & ", searchTerms);

        try
        {
            // Raw SQL ile tsvector sorgusu — performans için
            var results = await db.WikiPages
                .FromSqlRaw(@"
                    SELECT p.*
                    FROM kb.wiki_pages p
                    WHERE p.""IsDeleted"" = false
                      AND p.""Status"" = 'Published'
                      AND (
                          to_tsvector('turkish', COALESCE(p.""Title"", '') || ' ' || COALESCE(p.""ContentHtml"", ''))
                          @@ to_tsquery('simple', {0})
                      )
                    ORDER BY ts_rank(
                        to_tsvector('turkish', COALESCE(p.""Title"", '') || ' ' || COALESCE(p.""ContentHtml"", '')),
                        to_tsquery('simple', {0})
                    ) DESC
                    LIMIT {1}", tsQuery, request.MaxResults)
                .Include(p => p.Space)
                .Select(p => new WikiPageSearchDto(
                    p.Id.Value, p.Title, p.Slug,
                    p.Space.Name, p.Space.Slug,
                    p.ContentHtml.Length > 200 ? p.ContentHtml.Substring(0, 200) + "..." : p.ContentHtml,
                    p.Status,
                    p.UpdatedAt ?? p.CreatedAt))
                .ToListAsync(ct);

            return results;
        }
        catch (Exception ex)
        {
            // tsvector 'turkish' dili yüklü değilse fallback olarak LIKE
            logger.LogWarning(ex, "tsvector search failed, falling back to LIKE search");
            var term = request.SearchText.ToLowerInvariant();
            return await db.WikiPages
                .Include(p => p.Space)
                .Where(p => p.Status == WikiPageStatuses.Published)
                .Where(p =>
                    p.Title.ToLower().Contains(term) ||
                    p.ContentHtml.ToLower().Contains(term))
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(request.MaxResults)
                .Select(p => new WikiPageSearchDto(
                    p.Id.Value, p.Title, p.Slug,
                    p.Space.Name, p.Space.Slug,
                    p.ContentHtml.Length > 200 ? p.ContentHtml.Substring(0, 200) + "..." : p.ContentHtml,
                    p.Status,
                    p.UpdatedAt ?? p.CreatedAt))
                .ToListAsync(ct);
        }
    }
}

// ── TicketResolved → KB Taslağı ──────────────────────────────
/// <summary>
/// TicketResolvedEvent dinler → AI ile KB makalesi taslağı üretir.
/// Oluşturulan sayfa Draft durumunda "Destek" wiki space'ine kaydedilir.
/// </summary>
public sealed class TicketResolvedKbHandler(
    KnowledgeBaseDbContext db,
    ILlmService llmService,
    IPromptManager promptManager,
    ILogger<TicketResolvedKbHandler> logger)
    : INotificationHandler<TicketResolvedEvent>
{
    public async Task Handle(TicketResolvedEvent notification, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("TicketResolvedKbHandler: Ticket {TicketNumber} için KB taslağı oluşturuluyor",
                notification.TicketNumber);

            // 1. "Destek" wiki space'i bul veya oluştur
            var supportSpace = await db.WikiSpaces
                .FirstOrDefaultAsync(s => s.Slug == "destek-bilgi-bankasi", ct);

            if (supportSpace is null)
            {
                supportSpace = WikiSpace.Create(
                    "Destek Bilgi Bankası", "destek-bilgi-bankasi",
                    "Çözülmüş taleplerden otomatik oluşturulan bilgi bankası makaleleri",
                    null, "🎫");
                db.WikiSpaces.Add(supportSpace);
                await db.SaveChangesAsync(ct);
            }

            // 2. Prompt render
            string prompt;
            try
            {
                prompt = await promptManager.RenderAsync("kb-article-from-ticket", new
                {
                    TicketNumber = notification.TicketNumber,
                    notification.TicketId,
                    ResolvedAt = notification.ResolvedAt.ToString("dd.MM.yyyy HH:mm"),
                });
            }
            catch
            {
                // Prompt template yoksa varsayılan prompt kullan
                prompt = $"""
                    Aşağıdaki bilgilere dayanarak kısa ve net bir bilgi bankası makalesi oluştur.
                    Makale Türkçe olmalı. HTML formatında yaz.
                    Makale yapısı:
                    - Başlık (h2)
                    - Sorun Açıklaması (p)
                    - Çözüm Adımları (ol > li)
                    - Önemli Notlar (varsa)

                    Ticket Numarası: {notification.TicketNumber}
                    Çözüm Tarihi: {notification.ResolvedAt:dd.MM.yyyy}
                    """;
            }

            // 3. LLM ile makale üret
            var llmResponse = await llmService.ChatAsync(new ChatRequest
            {
                Messages = [ChatMessage.User(prompt)],
                MaxTokens = 2000,
                Temperature = 0.3f
            }, ct);

            var articleHtml = llmResponse.Content ?? "<p>İçerik üretilemedi.</p>";
            var articleJson = $"{{\"type\":\"doc\",\"content\":[{{\"type\":\"paragraph\",\"content\":[{{\"type\":\"text\",\"text\":\"{EscapeJsonString(articleHtml)}\"}}]}}]}}";

            // 4. Wiki sayfası oluştur (Draft)
            var title = $"{notification.TicketNumber} — Çözüm";
            var slug = GenerateSlug(title);

            // Slug uniqueness
            var slugExists = await db.WikiPages.AnyAsync(
                p => p.WikiSpaceId == supportSpace.Id && p.Slug == slug, ct);
            if (slugExists)
                slug = $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var page = WikiPage.Create(supportSpace.Id, title, slug,
                articleJson, articleHtml,
                notification.AssigneeUserId ?? Guid.Empty,
                sourceTicketId: notification.TicketId);

            db.WikiPages.Add(page);

            // İlk versiyon
            var version = WikiPageVersion.Create(page.Id, 1,
                articleJson, articleHtml,
                notification.AssigneeUserId ?? Guid.Empty,
                $"Ticket {notification.TicketNumber} çözümünden otomatik oluşturuldu");
            db.WikiPageVersions.Add(version);

            await db.SaveChangesAsync(ct);

            logger.LogInformation("TicketResolvedKbHandler: KB sayfası oluşturuldu — PageId={PageId}, Title={Title}",
                page.Id.Value, title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TicketResolvedKbHandler: Ticket {TicketNumber} için KB taslağı oluşturulamadı",
                notification.TicketNumber);
            // Hata fırlatma — event handler silently fail
        }
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace("ş", "s").Replace("ç", "c").Replace("ğ", "g")
            .Replace("ü", "u").Replace("ö", "o").Replace("ı", "i")
            .Replace("Ş", "s").Replace("Ç", "c").Replace("Ğ", "g")
            .Replace("Ü", "u").Replace("Ö", "o").Replace("İ", "i");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

    private static string EscapeJsonString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
}

// ── Requirement → Wiki Spec Dokümanı ─────────────────────────
/// <summary>
/// FeatureSpec requirement + altındaki tüm child gereksinimlerden
/// yapılandırılmış bir wiki spec dokümanı üretir.
/// </summary>
public sealed class GenerateWikiFromRequirementCommandHandler(
    KnowledgeBaseDbContext db,
    IMediator mediator,
    ILlmService llmService,
    IPromptManager promptManager,
    ILogger<GenerateWikiFromRequirementCommandHandler> logger)
    : IRequestHandler<GenerateWikiFromRequirementCommand, Guid>
{
    public async Task<Guid> Handle(GenerateWikiFromRequirementCommand request, CancellationToken ct)
    {
        // 1. Requirement + children çek
        var requirement = await mediator.Send(new GetRequirementQuery(request.RequirementId), ct)
            ?? throw new KeyNotFoundException($"Requirement {request.RequirementId} not found");

        // 2. Proje bilgisi çek
        var project = await mediator.Send(new GetProjectQuery(request.ProjectId), ct);
        var projectKey = project?.Key ?? "PRJ";

        // 3. Proje wiki space'i bul veya oluştur
        var projectSpace = await db.WikiSpaces
            .FirstOrDefaultAsync(s => s.ProjectId == request.ProjectId, ct);

        if (projectSpace is null)
        {
            projectSpace = WikiSpace.Create(
                $"{project?.Name ?? "Proje"} Wiki",
                $"prj-{projectKey.ToLowerInvariant()}-wiki",
                $"{project?.Name} proje dokümantasyonu",
                request.ProjectId, "📋");
            db.WikiSpaces.Add(projectSpace);
            await db.SaveChangesAsync(ct);
        }

        // 4. Prompt render — requirement verilerini model olarak geç
        string prompt;
        try
        {
            prompt = await promptManager.RenderAsync("wiki-spec-from-requirement", new
            {
                ProjectKey = projectKey,
                ProjectName = project?.Name ?? "",
                Requirement = requirement,
                Children = requirement.Children ?? [],
                WorkItems = requirement.WorkItems ?? [],
            });
        }
        catch
        {
            // Template yoksa fallback prompt oluştur
            prompt = BuildFallbackPrompt(requirement, projectKey, project?.Name);
        }

        // 5. LLM ile spec dokümanı üret
        var llmResponse = await llmService.ChatAsync(new ChatRequest
        {
            Messages = [ChatMessage.User(prompt)],
            MaxTokens = 4000,
            Temperature = 0.2f
        }, ct);

        var specHtml = llmResponse.Content ?? BuildManualSpec(requirement, projectKey);
        var specJson = $"{{\"type\":\"doc\",\"content\":[{{\"type\":\"paragraph\",\"content\":[{{\"type\":\"text\",\"text\":\"AI generated spec\"}}]}}]}}";

        // 6. Wiki sayfası oluştur
        var title = $"{requirement.Key} — {requirement.Title}";
        var slug = GenerateSlug(title);

        // Slug uniqueness
        var slugExists = await db.WikiPages.AnyAsync(
            p => p.WikiSpaceId == projectSpace.Id && p.Slug == slug, ct);
        if (slugExists)
            slug = $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        // Daha önce aynı requirement'tan wiki oluşturulduysa güncelle
        var existingPage = await db.WikiPages
            .FirstOrDefaultAsync(p => p.WikiSpaceId == projectSpace.Id
                && p.SourceRequirementId == request.RequirementId, ct);

        if (existingPage is not null)
        {
            // Yeni versiyon oluştur
            var lastVer = await db.WikiPageVersions
                .Where(v => v.WikiPageId == existingPage.Id)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => v.VersionNumber)
                .FirstOrDefaultAsync(ct);

            var ver = WikiPageVersion.Create(existingPage.Id, lastVer + 1,
                specJson, specHtml, Guid.Empty,
                $"Gereksinim güncelleme — {DateTime.UtcNow:dd.MM.yyyy HH:mm}");
            db.WikiPageVersions.Add(ver);

            existingPage.UpdateContent(specJson, specHtml, Guid.Empty, title);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("RequirementToWiki: Mevcut sayfa güncellendi — PageId={PageId}", existingPage.Id.Value);
            return existingPage.Id.Value;
        }

        // Yeni sayfa oluştur
        var page = WikiPage.Create(projectSpace.Id, title, slug,
            specJson, specHtml,
            Guid.Empty,
            sourceRequirementId: request.RequirementId);

        db.WikiPages.Add(page);

        var version = WikiPageVersion.Create(page.Id, 1,
            specJson, specHtml, Guid.Empty,
            $"Gereksinim {requirement.Key} spec dokümanından oluşturuldu");
        db.WikiPageVersions.Add(version);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("RequirementToWiki: Yeni wiki spec oluşturuldu — PageId={PageId}, Req={ReqKey}",
            page.Id.Value, requirement.Key);

        return page.Id.Value;
    }

    private static string BuildFallbackPrompt(RequirementDetailDto req, string projectKey, string? projectName)
    {
        var childrenTable = "";
        if (req.Children?.Count > 0)
        {
            childrenTable = string.Join("\n", req.Children.Select((c, i) =>
                $"| R{i + 1} | {c.Title} | {c.Priority} | {c.Status} |"));
        }

        return $"""
            Aşağıdaki gereksinim verilerinden Türkçe olarak yapılandırılmış bir HTML spec dokümanı oluştur.
            Doküman yapısı:
            1. Başlık (h1): "{req.Key} — {req.Title}"
            2. Proje ve durum bilgisi (p)
            3. Özet — gereksinimin açıklaması (section)
            4. Fonksiyonel Gereksinimler tablosu (table: #, Gereksinim, Öncelik, Durum)
            5. Kabul Kriterleri (section)
            6. İlgili iş kalemleri (varsa)

            Proje: {projectKey} - {projectName}
            Durum: {req.Status}
            Öncelik: {req.Priority}
            Tür: {req.Type}

            Açıklama:
            {req.Description ?? "(Açıklama yok)"}

            Kabul Kriterleri:
            {req.AcceptanceCriteria ?? "(Belirtilmemiş)"}

            Alt Gereksinimler:
            {(string.IsNullOrEmpty(childrenTable) ? "(Yok)" : childrenTable)}

            Tasarım Linki: {req.ExternalDesignUrl ?? "(Yok)"}
            Kaynak Ticket: {req.SourceTicketNumber ?? "(Yok)"}

            İlgili İş Kalemleri: {req.WorkItems?.Count ?? 0} adet
            """;
    }

    private static string BuildManualSpec(RequirementDetailDto req, string projectKey)
    {
        var html = $"""
            <h1>{req.Key} — {req.Title}</h1>
            <p><strong>Proje:</strong> {projectKey} | <strong>Durum:</strong> {req.Status} | <strong>Öncelik:</strong> {req.Priority}</p>
            <h2>Özet</h2>
            <p>{req.Description ?? "Açıklama girilmemiş."}</p>
            """;

        if (req.Children?.Count > 0)
        {
            html += "<h2>Fonksiyonel Gereksinimler</h2><table><tr><th>#</th><th>Gereksinim</th><th>Öncelik</th><th>Durum</th></tr>";
            for (int i = 0; i < req.Children.Count; i++)
            {
                var c = req.Children[i];
                html += $"<tr><td>R{i + 1}</td><td>{c.Title}</td><td>{c.Priority}</td><td>{c.Status}</td></tr>";
            }
            html += "</table>";
        }

        if (!string.IsNullOrEmpty(req.AcceptanceCriteria))
            html += $"<h2>Kabul Kriterleri</h2><p>{req.AcceptanceCriteria}</p>";

        return html;
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace("ş", "s").Replace("ç", "c").Replace("ğ", "g")
            .Replace("ü", "u").Replace("ö", "o").Replace("ı", "i")
            .Replace("Ş", "s").Replace("Ç", "c").Replace("Ğ", "g")
            .Replace("Ü", "u").Replace("Ö", "o").Replace("İ", "i");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}

// ── GenerateKbFromTicket (Manuel tetik) ──────────────────────
/// <summary>
/// Manuel olarak tetiklenen ticket → KB dönüşümü.
/// Endpoint: POST /api/v1/wiki/generate-from-ticket/{ticketId}
/// </summary>
public sealed class GenerateKbFromTicketCommandHandler(
    KnowledgeBaseDbContext db,
    ILlmService llmService,
    IPromptManager promptManager,
    ILogger<GenerateKbFromTicketCommandHandler> logger)
    : IRequestHandler<GenerateKbFromTicketCommand, Guid>
{
    public async Task<Guid> Handle(GenerateKbFromTicketCommand request, CancellationToken ct)
    {
        logger.LogInformation("GenerateKbFromTicket: {TicketNumber} için KB oluşturuluyor", request.TicketNumber);

        // "Destek" wiki space'i bul veya oluştur
        var supportSpace = await db.WikiSpaces
            .FirstOrDefaultAsync(s => s.Slug == "destek-bilgi-bankasi", ct);

        if (supportSpace is null)
        {
            supportSpace = WikiSpace.Create(
                "Destek Bilgi Bankası", "destek-bilgi-bankasi",
                "Çözülmüş taleplerden oluşturulan bilgi bankası makaleleri",
                null, "🎫");
            db.WikiSpaces.Add(supportSpace);
            await db.SaveChangesAsync(ct);
        }

        // Prompt
        string prompt;
        try
        {
            prompt = await promptManager.RenderAsync("kb-article-from-ticket", new
            {
                request.TicketNumber,
                request.Title,
                request.Description,
                request.Resolution,
            });
        }
        catch
        {
            prompt = $"""
                Aşağıdaki destek talebinden Türkçe bir bilgi bankası makalesi oluştur. HTML formatında yaz.
                Yapı: Başlık (h2), Sorun (p), Çözüm Adımları (ol), Notlar (varsa).

                Ticket: {request.TicketNumber}
                Başlık: {request.Title}
                Açıklama: {request.Description ?? "(Yok)"}
                Çözüm: {request.Resolution ?? "(Belirtilmemiş)"}
                """;
        }

        var llmResponse = await llmService.ChatAsync(new ChatRequest
        {
            Messages = [ChatMessage.User(prompt)],
            MaxTokens = 2000,
            Temperature = 0.3f
        }, ct);

        var articleHtml = llmResponse.Content ?? $"<h2>{request.Title}</h2><p>{request.Description}</p>";
        var articleJson = $"{{\"type\":\"doc\",\"content\":[{{\"type\":\"paragraph\"}}]}}";

        var title = $"{request.TicketNumber} — {request.Title}";
        var slug = GenerateSlug(title);

        var slugExists = await db.WikiPages.AnyAsync(
            p => p.WikiSpaceId == supportSpace.Id && p.Slug == slug, ct);
        if (slugExists)
            slug = $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var page = WikiPage.Create(supportSpace.Id, title, slug,
            articleJson, articleHtml,
            request.AssigneeUserId ?? Guid.Empty,
            sourceTicketId: request.TicketId);

        db.WikiPages.Add(page);

        var version = WikiPageVersion.Create(page.Id, 1,
            articleJson, articleHtml,
            request.AssigneeUserId ?? Guid.Empty,
            $"Ticket {request.TicketNumber} çözümünden oluşturuldu");
        db.WikiPageVersions.Add(version);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("GenerateKbFromTicket: KB sayfası oluşturuldu — PageId={PageId}", page.Id.Value);
        return page.Id.Value;
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace("ş", "s").Replace("ç", "c").Replace("ğ", "g")
            .Replace("ü", "u").Replace("ö", "o").Replace("ı", "i")
            .Replace("Ş", "s").Replace("Ç", "c").Replace("Ğ", "g")
            .Replace("Ü", "u").Replace("Ö", "o").Replace("İ", "i");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}

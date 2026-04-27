using EntApp.Modules.KnowledgeBase.Application.Commands;
using EntApp.Modules.KnowledgeBase.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.KnowledgeBase.Infrastructure.Endpoints;

/// <summary>KnowledgeBase REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class KnowledgeBaseEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeBaseEndpoints(this IEndpointRouteBuilder app)
    {
        // ── WikiSpace ──────────────────────────────────────────
        var spaces = app.MapGroup("/api/v1/wiki/spaces").WithTags("Wiki - Spaces");

        spaces.MapGet("/", async (ISender mediator, Guid? projectId) =>
            Results.Ok(await mediator.Send(new ListWikiSpacesQuery(projectId))))
            .WithName("ListWikiSpaces");

        spaces.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var r = await mediator.Send(new GetWikiSpaceQuery(id));
            return r is null ? Results.NotFound() : Results.Ok(r);
        }).WithName("GetWikiSpace");

        spaces.MapPost("/", async (CreateWikiSpaceRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateWikiSpaceCommand(
                req.Name, req.Slug, req.Description, req.ProjectId, req.IconEmoji));
            return Results.Created($"/api/v1/wiki/spaces/{id}", new { id });
        }).WithName("CreateWikiSpace");

        spaces.MapPut("/{id:guid}", async (Guid id, UpdateWikiSpaceRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateWikiSpaceCommand(id, req.Name, req.Description, req.IconEmoji));
            return Results.Ok(new { id });
        }).WithName("UpdateWikiSpace");

        spaces.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteWikiSpaceCommand(id));
            return Results.NoContent();
        }).WithName("DeleteWikiSpace");

        // ── WikiPage ───────────────────────────────────────────
        var pages = app.MapGroup("/api/v1/wiki/pages").WithTags("Wiki - Pages");

        pages.MapPost("/", async (CreateWikiPageRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateWikiPageCommand(
                req.SpaceId, req.Title, req.ContentJson, req.ContentHtml,
                req.ParentPageId, req.Status,
                req.SourceRequirementId, req.SourceTicketId));
            return Results.Created($"/api/v1/wiki/pages/{id}", new { id });
        }).WithName("CreateWikiPage");

        pages.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var r = await mediator.Send(new GetWikiPageQuery(id));
            return r is null ? Results.NotFound() : Results.Ok(r);
        }).WithName("GetWikiPage");

        pages.MapPut("/{id:guid}", async (Guid id, UpdateWikiPageRequest req, ISender mediator) =>
        {
            var pageId = await mediator.Send(new UpdateWikiPageCommand(
                id, req.Title, req.ContentJson, req.ContentHtml, req.ChangeNote));
            return Results.Ok(new { id = pageId });
        }).WithName("UpdateWikiPage");

        pages.MapPut("/{id:guid}/move", async (Guid id, MoveWikiPageRequest req, ISender mediator) =>
        {
            await mediator.Send(new MoveWikiPageCommand(id, req.NewParentPageId, req.NewSortOrder));
            return Results.Ok(new { id });
        }).WithName("MoveWikiPage");

        pages.MapPost("/{id:guid}/publish", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new PublishWikiPageCommand(id));
            return Results.Ok(new { id });
        }).WithName("PublishWikiPage");

        pages.MapPost("/{id:guid}/archive", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new ArchiveWikiPageCommand(id));
            return Results.Ok(new { id });
        }).WithName("ArchiveWikiPage");

        pages.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteWikiPageCommand(id));
            return Results.NoContent();
        }).WithName("DeleteWikiPage");

        pages.MapPost("/{id:guid}/lock", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new LockWikiPageCommand(id));
            return Results.Ok(new { id });
        }).WithName("LockWikiPage");

        pages.MapPost("/{id:guid}/unlock", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new UnlockWikiPageCommand(id));
            return Results.Ok(new { id });
        }).WithName("UnlockWikiPage");

        // ── Page Tree ──────────────────────────────────────────
        spaces.MapGet("/{spaceId:guid}/tree", async (Guid spaceId, ISender mediator) =>
            Results.Ok(await mediator.Send(new GetWikiPageTreeQuery(spaceId))))
            .WithName("GetWikiPageTree");

        // ── By Slug ────────────────────────────────────────────
        pages.MapGet("/by-slug/{spaceSlug}/{pageSlug}", async (string spaceSlug, string pageSlug, ISender mediator) =>
        {
            var r = await mediator.Send(new GetWikiPageBySlugQuery(spaceSlug, pageSlug));
            return r is null ? Results.NotFound() : Results.Ok(r);
        }).WithName("GetWikiPageBySlug");

        // ── Versions ───────────────────────────────────────────
        pages.MapGet("/{id:guid}/versions", async (Guid id, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListWikiPageVersionsQuery(id))))
            .WithName("ListWikiPageVersions");

        var versions = app.MapGroup("/api/v1/wiki/versions").WithTags("Wiki - Versions");
        versions.MapGet("/{versionId:guid}", async (Guid versionId, ISender mediator) =>
        {
            var r = await mediator.Send(new GetWikiPageVersionQuery(versionId));
            return r is null ? Results.NotFound() : Results.Ok(r);
        }).WithName("GetWikiPageVersion");

        pages.MapPost("/{id:guid}/revert/{versionId:guid}", async (Guid id, Guid versionId, ISender mediator) =>
        {
            var pageId = await mediator.Send(new RevertToVersionCommand(id, versionId));
            return Results.Ok(new { id = pageId });
        }).WithName("RevertToVersion");

        // ── Search ─────────────────────────────────────────────
        var search = app.MapGroup("/api/v1/wiki").WithTags("Wiki - Search");
        search.MapGet("/search", async (ISender mediator, string q,
            Guid? spaceId = null, Guid? projectId = null,
            int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new SearchWikiPagesQuery(q, spaceId, projectId, page, pageSize))))
            .WithName("SearchWikiPages");

        // ── Self-service Deflection (Suggest) ───────────────────
        search.MapGet("/suggest", async (ISender mediator, string q, int maxResults = 5) =>
            Results.Ok(await mediator.Send(new SuggestKbArticlesQuery(q, maxResults))))
            .WithName("SuggestKbArticles");

        // ── AI Entegrasyon ──────────────────────────────────────
        var generate = app.MapGroup("/api/v1/wiki").WithTags("Wiki - AI Generation");

        generate.MapPost("/generate-from-requirement/{requirementId:guid}",
            async (Guid requirementId, GenerateFromRequirementRequest req, ISender mediator) =>
        {
            var pageId = await mediator.Send(new GenerateWikiFromRequirementCommand(
                requirementId, req.ProjectId));
            return Results.Created($"/api/v1/wiki/pages/{pageId}", new { id = pageId });
        }).WithName("GenerateWikiFromRequirement");

        generate.MapPost("/generate-from-ticket/{ticketId:guid}",
            async (Guid ticketId, GenerateFromTicketRequest req, ISender mediator) =>
        {
            var pageId = await mediator.Send(new GenerateKbFromTicketCommand(
                ticketId, req.TicketNumber, req.Title,
                req.Description, req.Resolution,
                req.CategoryId, req.AssigneeUserId));
            return Results.Created($"/api/v1/wiki/pages/{pageId}", new { id = pageId });
        }).WithName("GenerateKbFromTicket");

        return app;
    }
}

// ── Request DTO'lar ─────────────────────────────────────────
public sealed record CreateWikiSpaceRequest(
    string Name, string Slug,
    string? Description = null, Guid? ProjectId = null,
    string? IconEmoji = null);

public sealed record UpdateWikiSpaceRequest(
    string? Name = null, string? Description = null,
    string? IconEmoji = null);

public sealed record CreateWikiPageRequest(
    Guid SpaceId, string Title,
    string ContentJson, string ContentHtml,
    Guid? ParentPageId = null, string? Status = null,
    Guid? SourceRequirementId = null, Guid? SourceTicketId = null);

public sealed record UpdateWikiPageRequest(
    string? Title = null,
    string? ContentJson = null, string? ContentHtml = null,
    string? ChangeNote = null);

public sealed record MoveWikiPageRequest(
    Guid? NewParentPageId = null, int? NewSortOrder = null);

public sealed record GenerateFromRequirementRequest(Guid ProjectId);

public sealed record GenerateFromTicketRequest(
    string TicketNumber, string Title,
    string? Description = null, string? Resolution = null,
    Guid? CategoryId = null, Guid? AssigneeUserId = null);

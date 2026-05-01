using EntApp.Modules.StateFlow.Application.Commands;
using EntApp.Modules.StateFlow.Application.Dtos;
using EntApp.Modules.StateFlow.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.StateFlow.Infrastructure.Endpoints;

/// <summary>StateFlow REST API endpoint'leri.</summary>
public static class StateFlowEndpoints
{
    public static IEndpointRouteBuilder MapStateFlowEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Flow Definitions ═══════════
        var flows = app.MapGroup("/api/sf/flows").WithTags("StateFlow - Definitions");

        flows.MapGet("/", async (ISender mediator, string? entityType, bool includeArchived = false) =>
            Results.Ok(await mediator.Send(new ListFlowDefinitionsQuery(entityType, includeArchived))))
            .WithName("ListFlowDefinitions");

        flows.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetFlowDefinitionQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetFlowDefinition");

        flows.MapGet("/published/{entityType}", async (string entityType, ISender mediator) =>
        {
            var result = await mediator.Send(new GetPublishedFlowQuery(entityType));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetPublishedFlow");

        flows.MapPost("/", async (CreateFlowRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateFlowDefinitionCommand(
                req.EntityType, req.Key, req.Name, req.Description, req.IsGlobalTemplate));
            return Results.Created($"/api/sf/flows/{id}", new { id });
        }).WithName("CreateFlowDefinition");

        flows.MapPut("/{id:guid}", async (Guid id, UpdateFlowRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateFlowDefinitionCommand(id, req.Name, req.Description));
            return Results.NoContent();
        }).WithName("UpdateFlowDefinition");

        flows.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteFlowDefinitionCommand(id));
            return Results.NoContent();
        }).WithName("DeleteFlowDefinition");

        // ═══════════ Lifecycle ═══════════
        flows.MapPost("/{id:guid}/publish", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new PublishFlowCommand(id));
            return Results.NoContent();
        }).WithName("PublishFlow");

        flows.MapPost("/{id:guid}/archive", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new ArchiveFlowCommand(id));
            return Results.NoContent();
        }).WithName("ArchiveFlow");

        flows.MapPost("/{id:guid}/new-version", async (Guid id, ISender mediator) =>
        {
            var newId = await mediator.Send(new CreateNewVersionCommand(id));
            return Results.Created($"/api/sf/flows/{newId}", new { id = newId });
        }).WithName("CreateNewFlowVersion");

        flows.MapPost("/{id:guid}/clone", async (Guid id, CloneFlowRequest? req, ISender mediator) =>
        {
            var cloneId = await mediator.Send(new CloneFromTemplateCommand(id, req?.CustomName));
            return Results.Created($"/api/sf/flows/{cloneId}", new { id = cloneId });
        }).WithName("CloneFromTemplate");

        // ═══════════ Designer ═══════════
        flows.MapPut("/{id:guid}/design", async (Guid id, SaveDesignRequest req, ISender mediator) =>
        {
            await mediator.Send(new SaveFlowDesignCommand(id, req.States, req.Transitions));
            return Results.NoContent();
        }).WithName("SaveFlowDesign");

        // ═══════════ Engine ═══════════
        var engine = app.MapGroup("/api/sf/engine").WithTags("StateFlow - Engine");

        engine.MapPost("/allowed-triggers", async (AllowedTriggersRequest req, ISender mediator) =>
            Results.Ok(await mediator.Send(new GetAllowedTriggersQuery(
                req.EntityType, req.CurrentState, req.FlowDefinitionId))))
            .WithName("GetAllowedTriggers");

        engine.MapPost("/validate", async (ValidateTransitionRequest req, ISender mediator) =>
            Results.Ok(new { isValid = await mediator.Send(new ValidateTransitionQuery(
                req.EntityType, req.CurrentState, req.Trigger, req.FlowDefinitionId)) }))
            .WithName("ValidateTransition");

        engine.MapPost("/fire", async (FireTransitionRequest req, ISender mediator) =>
        {
            var newState = await mediator.Send(new FireTransitionCommand(
                req.EntityType, req.CurrentState, req.Trigger, req.FlowDefinitionId));
            return Results.Ok(new { newState });
        }).WithName("FireTransition");

        // ═══════════ Automation — Execution Logs ═══════════
        var automation = app.MapGroup("/api/sf/automation").WithTags("StateFlow - Automation");

        automation.MapGet("/logs", async (ISender mediator, Guid? flowDefinitionId, Guid? entityId, int limit = 50) =>
            Results.Ok(await mediator.Send(new ListRuleExecutionLogsQuery(flowDefinitionId, entityId, limit))))
            .WithName("ListRuleExecutionLogs");

        automation.MapGet("/action-types", () =>
            Results.Ok(new List<object>
            {
                new { type = "SendNotification", label = "Bildirim Gönder", description = "InApp veya Email bildirim gönderir", paramFields = new[] { "channel: InApp|Email", "template: string", "recipientRole: Assignee|ProjectManager|Reporter" } },
                new { type = "AddComment", label = "Yorum Ekle", description = "Entity'ye otomatik yorum ekler", paramFields = new[] { "content: string" } },
                new { type = "AssignWorkItem", label = "Atama Yap", description = "İş kalemini belirtilen kişiye atar", paramFields = new[] { "userId: guid", "role: string?" } },
                new { type = "ChangeStatus", label = "Durum Değiştir", description = "İlişkili entity'nin durumunu değiştirir", paramFields = new[] { "status: string", "targetEntityType: string?" } },
            }))
            .WithName("GetSupportedActionTypes");

        // ═══════════ Automation — Event Rules CRUD ═══════════

        automation.MapGet("/event-rules", async (ISender mediator, bool? enabledOnly) =>
            Results.Ok(await mediator.Send(new ListEventRulesQuery(enabledOnly))))
            .WithName("ListEventRules");

        automation.MapPost("/event-rules", async (ISender mediator, CreateEventRuleRequest req) =>
        {
            var id = await mediator.Send(new CreateEventRuleCommand(
                req.Name, req.TriggerType, req.ActionType,
                req.Description, req.TriggerConditions,
                req.ActionParams, req.EntityType,
                req.Priority, req.SortOrder));
            return Results.Created($"/api/sf/automation/event-rules/{id}", new { id });
        }).WithName("CreateEventRule");

        automation.MapPut("/event-rules/{id:guid}", async (ISender mediator, Guid id, UpdateEventRuleRequest req) =>
        {
            await mediator.Send(new UpdateEventRuleCommand(
                id, req.Name, req.TriggerType, req.ActionType,
                req.Description, req.TriggerConditions,
                req.ActionParams, req.EntityType,
                req.Priority, req.SortOrder));
            return Results.NoContent();
        }).WithName("UpdateEventRule");

        automation.MapPatch("/event-rules/{id:guid}/toggle", async (ISender mediator, Guid id) =>
        {
            await mediator.Send(new ToggleEventRuleCommand(id));
            return Results.NoContent();
        }).WithName("ToggleEventRule");

        automation.MapDelete("/event-rules/{id:guid}", async (ISender mediator, Guid id) =>
        {
            await mediator.Send(new DeleteEventRuleCommand(id));
            return Results.NoContent();
        }).WithName("DeleteEventRule");

        // ═══════════ Automation — Trigger Types Reference ═══════════

        automation.MapGet("/trigger-types", () =>
            Results.Ok(new List<object>
            {
                new { type = "SLAResponseBreached", label = "SLA Yanıt Süresi Aşımı", description = "SLA yanıt süresi aşıldığında tetiklenir" },
                new { type = "SLAResolutionBreached", label = "SLA Çözüm Süresi Aşımı", description = "SLA çözüm süresi aşıldığında tetiklenir" },
                new { type = "TicketIdleTimeout", label = "Ticket Bekleme Zaman Aşımı", description = "Ticket belirli süre atanmadan/güncellenmeden beklediğinde tetiklenir" },
                new { type = "PriorityChanged", label = "Öncelik Değişikliği", description = "Entity önceliği değiştiğinde tetiklenir" },
                new { type = "AssignmentChanged", label = "Atama Değişikliği", description = "Entity ataması değiştiğinde tetiklenir" },
                new { type = "EntityCreated", label = "Entity Oluşturuldu", description = "Yeni entity oluşturulduğunda tetiklenir" },
                new { type = "EntityUpdated", label = "Entity Güncellendi", description = "Entity güncellendiğinde tetiklenir" },
                new { type = "CommentAdded", label = "Yorum Eklendi", description = "Entity'ye yorum eklendiğinde tetiklenir" },
            }))
            .WithName("GetSupportedTriggerTypes");

        return app;
    }
}

// ── Request DTOs ──────────────────────────────────────────────
public sealed record CreateFlowRequest(
    string EntityType, string Key, string Name,
    string? Description = null, bool IsGlobalTemplate = false);

public sealed record UpdateFlowRequest(string Name, string? Description);

public sealed record CloneFlowRequest(string? CustomName);

public sealed record SaveDesignRequest(
    IReadOnlyList<StateDto> States,
    IReadOnlyList<TransitionDto> Transitions);

public sealed record AllowedTriggersRequest(
    string EntityType, string CurrentState, Guid FlowDefinitionId);

public sealed record ValidateTransitionRequest(
    string EntityType, string CurrentState, string Trigger, Guid FlowDefinitionId);

public sealed record FireTransitionRequest(
    string EntityType, string CurrentState, string Trigger, Guid FlowDefinitionId);

// ── Event Automation Rule Request DTOs ────────────────────────
public sealed record CreateEventRuleRequest(
    string Name, string TriggerType, string ActionType,
    string? Description = null, string? TriggerConditions = null,
    string? ActionParams = null, string? EntityType = null,
    int Priority = 0, int SortOrder = 0);

public sealed record UpdateEventRuleRequest(
    string Name, string TriggerType, string ActionType,
    string? Description, string? TriggerConditions,
    string? ActionParams, string? EntityType,
    int Priority, int SortOrder);

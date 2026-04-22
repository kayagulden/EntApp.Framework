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

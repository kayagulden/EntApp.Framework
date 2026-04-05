using EntApp.Modules.RequestManagement.Application.Commands;
using EntApp.Modules.RequestManagement.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.RequestManagement.Infrastructure.Endpoints;

/// <summary>ServiceQueue REST API endpoint'leri.</summary>
public static class ServiceQueueEndpoints
{
    public static IEndpointRouteBuilder MapServiceQueueEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Queues ═══════════
        var queues = app.MapGroup("/api/req/queues").WithTags("Request Mgmt - Service Queues");

        queues.MapGet("/", async (ISender mediator, Guid? departmentId, bool? activeOnly) =>
            Results.Ok(await mediator.Send(new ListServiceQueuesQuery(departmentId, activeOnly ?? true))))
            .WithName("ListServiceQueues");

        queues.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetServiceQueueQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetServiceQueue");

        queues.MapPost("/", async (CreateQueueRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateServiceQueueCommand(
                req.Name, req.Code, req.Description,
                req.DepartmentId, req.ManagerUserId, req.DefaultWorkflowDefinitionId));
            return Results.Created($"/api/req/queues/{id}", new { id });
        }).WithName("CreateServiceQueue");

        queues.MapPut("/{id:guid}", async (Guid id, UpdateQueueRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateServiceQueueCommand(
                id, req.Name, req.Code, req.Description,
                req.DepartmentId, req.ManagerUserId, req.DefaultWorkflowDefinitionId));
            return Results.NoContent();
        }).WithName("UpdateServiceQueue");

        // ═══════════ Queue Members ═══════════
        queues.MapPost("/{queueId:guid}/members", async (Guid queueId, AddMemberRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new AddQueueMemberCommand(queueId, req.UserId, req.Role));
            return Results.Created($"/api/req/queues/{queueId}/members/{id}", new { id });
        }).WithName("AddQueueMember");

        queues.MapDelete("/members/{membershipId:guid}", async (Guid membershipId, ISender mediator) =>
        {
            await mediator.Send(new RemoveQueueMemberCommand(membershipId));
            return Results.NoContent();
        }).WithName("RemoveQueueMember");

        queues.MapPut("/members/{membershipId:guid}/role", async (Guid membershipId, UpdateMemberRoleRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateQueueMemberRoleCommand(membershipId, req.Role));
            return Results.NoContent();
        }).WithName("UpdateQueueMemberRole");

        return app;
    }
}

// ── Request DTOs ──────────────────────────────────────────────
public sealed record CreateQueueRequest(string Name, string Code, string? Description, Guid? DepartmentId, Guid? ManagerUserId, Guid? DefaultWorkflowDefinitionId);
public sealed record UpdateQueueRequest(string Name, string Code, string? Description, Guid? DepartmentId, Guid? ManagerUserId, Guid? DefaultWorkflowDefinitionId);
public sealed record AddMemberRequest(Guid UserId, string Role = "Member");
public sealed record UpdateMemberRoleRequest(string Role);

using EntApp.Modules.MyModule.Application.Commands;
using EntApp.Modules.MyModule.Application.Queries;
using EntApp.Modules.MyModule.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.MyModule.Infrastructure.Endpoints;

/// <summary>
/// MyModule REST API endpoint'leri.
/// Thin proxy pattern — tüm iş mantığı MediatR handler'larında yaşar.
/// </summary>
public static class MyModuleEndpoints
{
    public static IEndpointRouteBuilder MapMyModuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/moduleschema/sample-entities")
            .WithTags("MyModule - SampleEntities");

        // ── List ──────────────────────────────────────────────────
        group.MapGet("/", async (ISender mediator,
            string? search, int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListSampleEntitiesQuery(search, page, pageSize));
            return Results.Ok(result);
        });

        // ── Get by ID ────────────────────────────────────────────
        group.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetSampleEntityQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // ── Create ───────────────────────────────────────────────
        group.MapPost("/", async (CreateSampleRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateSampleEntityCommand(req.Name, req.Description));
            return Results.Created($"/api/moduleschema/sample-entities/{result.Id}", result);
        });

        // ── Update ───────────────────────────────────────────────
        group.MapPut("/{id:guid}", async (Guid id, UpdateSampleRequest req, ISender mediator) =>
        {
            try
            {
                await mediator.Send(new UpdateSampleEntityCommand(id, req.Name, req.Description, req.Status));
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        // ── Delete ───────────────────────────────────────────────
        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            try
            {
                await mediator.Send(new DeleteSampleEntityCommand(id));
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        return app;
    }

    // ── Request DTO'lar ──────────────────────────────────────────
    public record CreateSampleRequest(string Name, string? Description);
    public record UpdateSampleRequest(string Name, string? Description, SampleStatus Status);
}

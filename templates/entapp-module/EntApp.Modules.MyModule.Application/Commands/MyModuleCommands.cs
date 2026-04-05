using EntApp.Modules.MyModule.Domain.Enums;
using MediatR;

namespace EntApp.Modules.MyModule.Application.Commands;

/// <summary>
/// Modül command tanımları — her IRequest bir MediatR command'dır.
/// Handler'lar Infrastructure/Handlers altında implemente edilir.
/// </summary>

public sealed record CreateSampleEntityCommand(
    string Name, string? Description) : IRequest<CreateSampleEntityResult>;
public sealed record CreateSampleEntityResult(Guid Id, string Name);

public sealed record UpdateSampleEntityCommand(
    Guid Id, string Name, string? Description,
    SampleStatus Status) : IRequest<bool>;

public sealed record DeleteSampleEntityCommand(Guid Id) : IRequest<bool>;

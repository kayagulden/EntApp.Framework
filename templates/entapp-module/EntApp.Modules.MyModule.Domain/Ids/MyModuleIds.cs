using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.MyModule.Domain.Ids;

/// <summary>Strongly Typed ID tanımları.</summary>
public readonly record struct SampleEntityId(Guid Value) : IEntityId;

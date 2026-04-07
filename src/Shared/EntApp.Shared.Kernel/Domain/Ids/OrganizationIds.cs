using EntApp.Shared.Kernel.Domain;

namespace EntApp.Shared.Kernel.Domain.Ids;

/// <summary>Organizasyon ID (strongly typed).</summary>
public readonly record struct OrganizationId(Guid Value) : IEntityId;

/// <summary>Departman ID (strongly typed). Tüm modüller tarafından paylaşılır.</summary>
public readonly record struct DepartmentId(Guid Value) : IEntityId;

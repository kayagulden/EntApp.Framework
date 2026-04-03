namespace EntApp.Modules.IAM.Application.DTOs;

/// <summary>Kullanıcı listesi DTO.</summary>
public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? PhoneNumber,
    string Status,
    Guid? OrganizationId,
    Guid? DepartmentId,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Roles);

/// <summary>Rol DTO.</summary>
public sealed record RoleDto(
    Guid Id,
    string Name,
    string DisplayName,
    string? Description,
    bool IsSystemRole,
    IReadOnlyList<string> Permissions);

/// <summary>Permission DTO.</summary>
public sealed record PermissionDto(
    Guid Id,
    string SystemName,
    string DisplayName,
    string Module,
    string? Description);

/// <summary>Organizasyon DTO.</summary>
public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentId,
    bool IsActive,
    IReadOnlyList<OrganizationDto> Children);

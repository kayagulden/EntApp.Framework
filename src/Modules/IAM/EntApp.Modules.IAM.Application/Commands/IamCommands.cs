using EntApp.Shared.Kernel.Results;
using MediatR;

namespace EntApp.Modules.IAM.Application.Commands;

/// <summary>Yeni kullanıcı oluştur.</summary>
public sealed record CreateUserCommand(
    string KeycloakId,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber = null) : IRequest<Result<Guid>>;

/// <summary>Kullanıcı bilgilerini güncelle.</summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber = null) : IRequest<Result>;

/// <summary>Kullanıcıya rol ata.</summary>
public sealed record AssignRoleCommand(
    Guid UserId,
    Guid RoleId) : IRequest<Result>;

/// <summary>Kullanıcıyı deaktif et.</summary>
public sealed record DeactivateUserCommand(
    Guid UserId) : IRequest<Result>;

/// <summary>Yeni rol oluştur.</summary>
public sealed record CreateRoleCommand(
    string Name,
    string DisplayName,
    string? Description = null) : IRequest<Result<Guid>>;

/// <summary>Yeni organizasyon oluştur.</summary>
public sealed record CreateOrganizationCommand(
    string Name,
    string Code,
    Guid? ParentId = null) : IRequest<Result<Guid>>;

using EntApp.Modules.IAM.Application.Commands;
using EntApp.Modules.IAM.Domain.Entities;
using EntApp.Modules.IAM.Infrastructure.Persistence;
using EntApp.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.IAM.Infrastructure.Handlers;

// ─── CreateUser ─────────────────────────────────────────────
public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IamDbContext _db;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(IamDbContext db, ILogger<CreateUserHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken))
        {
            return Result<Guid>.Failure(Error.Conflict("User.EmailExists", "Bu e-posta adresi zaten kullanımda."));
        }

        if (await _db.Users.AnyAsync(u => u.UserName == request.UserName, cancellationToken))
        {
            return Result<Guid>.Failure(Error.Conflict("User.UserNameExists", "Bu kullanıcı adı zaten kullanımda."));
        }

        var user = User.Create(
            request.KeycloakId,
            request.UserName,
            request.Email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[IAM] User created: {UserId} ({UserName})", user.Id, user.UserName);
        return Result<Guid>.Success(user.Id);
    }
}

// ─── UpdateUser ─────────────────────────────────────────────
public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IamDbContext _db;

    public UpdateUserHandler(IamDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([request.UserId], cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("User.NotFound", "Kullanıcı bulunamadı."));
        }

        user.Update(request.FirstName, request.LastName, request.PhoneNumber);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ─── AssignRole ─────────────────────────────────────────────
public sealed class AssignRoleHandler : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IamDbContext _db;
    private readonly ILogger<AssignRoleHandler> _logger;

    public AssignRoleHandler(IamDbContext db, ILogger<AssignRoleHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(Error.NotFound("User.NotFound", "Kullanıcı bulunamadı."));
        }

        var role = await _db.Roles.FindAsync([request.RoleId], cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound("Role.NotFound", "Rol bulunamadı."));
        }

        user.AssignRole(role);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[IAM] Role assigned: {RoleName} → {UserName}", role.Name, user.UserName);
        return Result.Success();
    }
}

// ─── DeactivateUser ─────────────────────────────────────────
public sealed class DeactivateUserHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IamDbContext _db;
    private readonly ILogger<DeactivateUserHandler> _logger;

    public DeactivateUserHandler(IamDbContext db, ILogger<DeactivateUserHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([request.UserId], cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("User.NotFound", "Kullanıcı bulunamadı."));
        }

        user.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[IAM] User deactivated: {UserId} ({UserName})", user.Id, user.UserName);
        return Result.Success();
    }
}

// ─── CreateRole ─────────────────────────────────────────────
public sealed class CreateRoleHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IamDbContext _db;

    public CreateRoleHandler(IamDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken))
        {
            return Result<Guid>.Failure(Error.Conflict("Role.NameExists", "Bu rol adı zaten kullanımda."));
        }

        var role = Role.Create(request.Name, request.DisplayName, request.Description);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(role.Id);
    }
}

// ─── CreateOrganization ─────────────────────────────────────
public sealed class CreateOrganizationHandler : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
{
    private readonly IamDbContext _db;

    public CreateOrganizationHandler(IamDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        if (await _db.Organizations.AnyAsync(o => o.Code == request.Code.ToUpperInvariant(), cancellationToken))
        {
            return Result<Guid>.Failure(Error.Conflict("Org.CodeExists", "Bu organizasyon kodu zaten kullanımda."));
        }

        if (request.ParentId.HasValue &&
            !await _db.Organizations.AnyAsync(o => o.Id == request.ParentId.Value, cancellationToken))
        {
            return Result<Guid>.Failure(Error.NotFound("Org.ParentNotFound", "Üst organizasyon bulunamadı."));
        }

        var org = Organization.Create(request.Name, request.Code, request.ParentId);
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(org.Id);
    }
}

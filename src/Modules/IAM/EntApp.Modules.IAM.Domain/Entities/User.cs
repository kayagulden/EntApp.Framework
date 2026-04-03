using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.IAM.Domain.Entities;

/// <summary>
/// IAM User entity — Keycloak ile senkronize kullanıcı.
/// AggregateRoot olarak domain event'leri destekler.
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    /// <summary>Keycloak subject ID (sub claim).</summary>
    public string KeycloakId { get; private set; } = null!;

    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; private set; } = null!;

    /// <summary>E-posta.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>Ad.</summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>Soyad.</summary>
    public string LastName { get; private set; } = null!;

    /// <summary>Ad Soyad.</summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Telefon.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Departman.</summary>
    public Guid? DepartmentId { get; private set; }
    public Department? Department { get; private set; }

    /// <summary>Organizasyon.</summary>
    public Guid? OrganizationId { get; private set; }
    public Organization? Organization { get; private set; }

    /// <summary>Kullanıcı durumu.</summary>
    public UserStatus Status { get; private set; } = UserStatus.Active;

    /// <summary>Son giriş zamanı.</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>Kullanıcı-Rol ilişkisi.</summary>
    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() { } // EF Core

    public static User Create(
        string keycloakId,
        string userName,
        string email,
        string firstName,
        string lastName,
        string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keycloakId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var user = new User
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakId,
            UserName = userName,
            Email = email.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        user.AddDomainEvent(new Events.UserCreatedEvent(user.Id, userName, email));
        return user;
    }

    public void Update(string firstName, string lastName, string? phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        if (_userRoles.Any(ur => ur.RoleId == role.Id)) return;

        _userRoles.Add(new UserRole { UserId = Id, RoleId = role.Id });
        AddDomainEvent(new Events.RoleAssignedEvent(Id, role.Id, role.Name));
    }

    public void RemoveRole(Guid roleId)
    {
        var ur = _userRoles.FirstOrDefault(x => x.RoleId == roleId);
        if (ur is not null) _userRoles.Remove(ur);
    }

    public void AssignToOrganization(Guid organizationId, Guid? departmentId = null)
    {
        OrganizationId = organizationId;
        DepartmentId = departmentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new Events.UserDeactivatedEvent(Id, UserName));
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}

/// <summary>Many-to-many join entity: User ↔ Role.</summary>
public sealed class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Kullanıcı durumu.</summary>
public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Locked = 3,
    PendingApproval = 4
}

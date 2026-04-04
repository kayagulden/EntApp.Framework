using EntApp.Modules.IAM.Domain.Entities;
using EntApp.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.IAM.Infrastructure.Persistence;

/// <summary>
/// IAM varsayılan rolleri seed data provider.
/// </summary>
public sealed class IamRoleSeedDataProvider : ISeedDataProvider
{
    public int Order => 100; // IAM seeds early
    public string Name => "IAM:DefaultRoles";

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IamDbContext>();

        // Seed varsayılan roller (idempotent)
        if (await dbContext.Roles.AnyAsync(cancellationToken))
        {
            return; // Zaten seed edilmiş
        }

        var roles = new[]
        {
            Role.Create("admin", "Sistem Yöneticisi", "Tüm yetkilere sahip", isSystemRole: true),
            Role.Create("manager", "Yönetici", "Departman/modül yönetimi", isSystemRole: true),
            Role.Create("user", "Kullanıcı", "Standart kullanıcı erişimi", isSystemRole: true),
            Role.Create("readonly", "Salt Okunur", "Yalnızca okuma yetkisi", isSystemRole: true)
        };

        dbContext.Roles.AddRange(roles);

        // Seed varsayılan IAM izinleri
        var permissions = new[]
        {
            Permission.Create("users.create", "Kullanıcı Oluştur", "IAM"),
            Permission.Create("users.read", "Kullanıcı Görüntüle", "IAM"),
            Permission.Create("users.update", "Kullanıcı Güncelle", "IAM"),
            Permission.Create("users.delete", "Kullanıcı Sil", "IAM"),
            Permission.Create("roles.manage", "Rol Yönetimi", "IAM"),
            Permission.Create("organizations.manage", "Organizasyon Yönetimi", "IAM")
        };

        dbContext.Permissions.AddRange(permissions);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

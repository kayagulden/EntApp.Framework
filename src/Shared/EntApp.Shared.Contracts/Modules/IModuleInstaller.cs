using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Shared.Contracts.Modules;

/// <summary>
/// Her modülün implement etmesi gereken DI kurulum kontratı.
/// ModuleRegistration tarafından assembly taraması ile otomatik keşfedilir.
/// </summary>
public interface IModuleInstaller
{
    /// <summary>Modül adı (log ve health check'lerde kullanılır).</summary>
    string ModuleName { get; }

    /// <summary>Modülün servislerini DI container'a kaydeder.</summary>
    void Install(IServiceCollection services, IConfiguration configuration);
}

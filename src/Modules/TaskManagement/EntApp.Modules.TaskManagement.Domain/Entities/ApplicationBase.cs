using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Uygulama kaydı — ConfigurationItemBase'den türer.
/// Yazılım/sistem'e özgü alanlar burada tanımlanır.
/// TPT: configuration_items (base) + applications (derived) tabloları.
/// </summary>
[DynamicEntity("Application", MenuGroup = "Proje Yönetimi")]
public sealed class ApplicationBase : ConfigurationItemBase
{
    /// <summary>Uygulama tipi (İç geliştirme, Paket, Altyapı, Hibrit).</summary>
    public ApplicationType ApplicationType { get; private set; } = ApplicationType.InHouse;

    /// <summary>Teknik sorumlu.</summary>
    public Guid? TechLeadUserId { get; private set; }

    /// <summary>Teknoloji yığını (serbest metin veya virgülle ayrılmış tag).</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 500)]
    public string? TechnologyStack { get; private set; }

    /// <summary>Kaynak kod deposu URL'i.</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 500)]
    public string? RepositoryUrl { get; private set; }

    /// <summary>Dokümantasyon URL'i.</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 500)]
    public string? DocumentationUrl { get; private set; }

    /// <summary>Mevcut sürüm.</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 50)]
    public string? CurrentVersion { get; private set; }

    private ApplicationBase() { }

    public static ApplicationBase Create(string name, string code, string? description = null,
        ApplicationType applicationType = ApplicationType.InHouse,
        CICriticality criticality = CICriticality.Medium,
        Guid? ownerUserId = null, Guid? techLeadUserId = null,
        string? technologyStack = null, string? repositoryUrl = null,
        string? documentationUrl = null, string? currentVersion = null)
    {
        return new ApplicationBase
        {
            Id = EntityId.New<ConfigurationItemId>(),
            Name = name,
            Code = code.ToUpperInvariant(),
            Description = description,
            ApplicationType = applicationType,
            Criticality = criticality,
            OwnerUserId = ownerUserId,
            TechLeadUserId = techLeadUserId,
            TechnologyStack = technologyStack,
            RepositoryUrl = repositoryUrl,
            DocumentationUrl = documentationUrl,
            CurrentVersion = currentVersion
        };
    }

    public void Update(string? name = null, string? description = null,
        ApplicationType? applicationType = null, CIStatus? status = null,
        CICriticality? criticality = null,
        Guid? ownerUserId = null, Guid? techLeadUserId = null,
        string? technologyStack = null, string? repositoryUrl = null,
        string? documentationUrl = null, string? currentVersion = null)
    {
        // Ortak CI alanlarını base'e delege et
        UpdateBase(name, description, status, criticality, ownerUserId);

        // Uygulamaya özgü alanlar
        if (applicationType.HasValue) ApplicationType = applicationType.Value;
        if (techLeadUserId.HasValue) TechLeadUserId = techLeadUserId.Value;
        if (technologyStack is not null) TechnologyStack = technologyStack;
        if (repositoryUrl is not null) RepositoryUrl = repositoryUrl;
        if (documentationUrl is not null) DocumentationUrl = documentationUrl;
        if (currentVersion is not null) CurrentVersion = currentVersion;
    }

    public void Activate() => Status = CIStatus.Active;
    public void Deprecate() => Status = CIStatus.Deprecated;
    public void Retire() => Status = CIStatus.Retired;
}

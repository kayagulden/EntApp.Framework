using EntApp.Modules.AI.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.AI.Domain.Entities;

/// <summary>
/// Sistemde tanımlı AI model kayıtları.
/// Hangi provider'da hangi model kullanılacağını belirler.
/// </summary>
[DynamicEntity("AiModel", MenuGroup = "AI Yönetimi")]
public sealed class AiModel : AuditableEntity<Guid>, ITenantEntity
{
    /// <summary>Sağlayıcı (OpenAI, Anthropic, vb.)</summary>
    [DynamicField(FieldType = FieldType.Enum)]
    public AiProvider Provider { get; private set; }

    /// <summary>API model adı (ör: gpt-4o-mini, claude-sonnet-4-20250514)</summary>
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 100)]
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>Kullanıcı dostu gösterim adı</summary>
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 150)]
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Model tipi (Chat, Embedding, Vision)</summary>
    [DynamicField(FieldType = FieldType.Enum)]
    public AiModelType ModelType { get; private set; }

    /// <summary>Maksimum token limiti</summary>
    [DynamicField(FieldType = FieldType.Number)]
    public int MaxTokens { get; private set; } = 4096;

    /// <summary>Aktif mi?</summary>
    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsActive { get; private set; } = true;

    /// <summary>Bu tip için varsayılan model mi?</summary>
    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsDefault { get; private set; }

    public Guid TenantId { get; set; }

    private AiModel() { }

    public static AiModel Create(
        AiProvider provider,
        string modelName,
        string displayName,
        AiModelType modelType,
        int maxTokens = 4096,
        bool isDefault = false)
    {
        return new AiModel
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ModelName = modelName,
            DisplayName = displayName,
            ModelType = modelType,
            MaxTokens = maxTokens,
            IsDefault = isDefault,
        };
    }
}

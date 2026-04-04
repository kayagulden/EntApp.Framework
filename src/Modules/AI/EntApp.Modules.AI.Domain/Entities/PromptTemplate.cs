using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.AI.Domain.Entities;

/// <summary>
/// Prompt şablonu — Scriban template engine ile render edilir.
/// Aynı key'de farklı versiyonlar tutulabilir.
/// </summary>
[DynamicEntity("PromptTemplate", MenuGroup = "AI Yönetimi")]
public sealed class PromptTemplate : AuditableEntity<Guid>, ITenantEntity
{
    /// <summary>Benzersiz şablon anahtarı (ör: "order-summary", "customer-greeting")</summary>
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 100, Searchable = true)]
    public string Key { get; private set; } = string.Empty;

    /// <summary>Versiyon numarası (aynı key, farklı versiyon)</summary>
    [DynamicField(FieldType = FieldType.Number)]
    public int Version { get; private set; } = 1;

    /// <summary>Kullanıcı dostu başlık</summary>
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Title { get; private set; } = string.Empty;

    /// <summary>Scriban template içeriği — {{variable}} destekler</summary>
    [DynamicField(FieldType = FieldType.Text, Required = true, MaxLength = 10000)]
    public string TemplateContent { get; private set; } = string.Empty;

    /// <summary>Kategori (ör: "CRM", "HR", "System")</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 50, Searchable = true)]
    public string? Category { get; private set; }

    /// <summary>Aktif mi?</summary>
    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    private PromptTemplate() { }

    public static PromptTemplate Create(
        string key,
        string title,
        string templateContent,
        string? category = null,
        int version = 1)
    {
        return new PromptTemplate
        {
            Id = Guid.NewGuid(),
            Key = key,
            Version = version,
            Title = title,
            TemplateContent = templateContent,
            Category = category,
        };
    }
}

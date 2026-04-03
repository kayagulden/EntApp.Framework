namespace EntApp.Shared.Kernel.Domain;

/// <summary>
/// Denetim bilgisi taşıyan entity'ler için base sınıf.
/// CreatedBy ve ModifiedBy alanlarını ekler.
/// </summary>
public abstract class AuditableEntity<TId> : BaseEntity<TId> where TId : struct
{
    public string? CreatedBy { get; set; }

    public string? ModifiedBy { get; set; }

    protected AuditableEntity() { }

    protected AuditableEntity(TId id) : base(id) { }
}

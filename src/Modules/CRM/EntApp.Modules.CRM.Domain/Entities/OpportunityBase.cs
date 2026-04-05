using EntApp.Modules.CRM.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.CRM.Domain.Entities;

/// <summary>Satış fırsatı — pipeline yönetimi.</summary>
[DynamicEntity("Opportunity", MenuGroup = "CRM")]
public sealed class OpportunityBase : AuditableEntity<Guid>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.Lookup, Required = true)]
    [DynamicLookup(typeof(CustomerBase), DisplayField = "Name")]
    public Guid CustomerId { get; private set; }

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Title { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 2000)]
    public string? Description { get; private set; }

    [DynamicField(FieldType = FieldType.Number)]
    public decimal EstimatedValue { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    [DynamicField(FieldType = FieldType.Enum)]
    public OpportunityStage Stage { get; private set; } = OpportunityStage.Lead;

    [DynamicField(FieldType = FieldType.Number)]
    public int Probability { get; private set; } = 10;

    [DynamicField(FieldType = FieldType.Date)]
    public DateTime? ExpectedCloseDate { get; private set; }

    [DynamicField(FieldType = FieldType.Date)]
    public DateTime? ActualCloseDate { get; private set; }

    [DynamicField(FieldType = FieldType.Lookup)]
    public Guid? AssignedUserId { get; private set; }
    public string? LostReason { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public CustomerBase Customer { get; private set; } = null!;

    private OpportunityBase() { }

    public static OpportunityBase Create(
        Guid customerId, string title, decimal estimatedValue = 0,
        string currency = "TRY", OpportunityStage stage = OpportunityStage.Lead,
        string? description = null, DateTime? expectedCloseDate = null,
        Guid? assignedUserId = null)
    {
        return new OpportunityBase
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId, Title = title,
            EstimatedValue = estimatedValue, Currency = currency,
            Stage = stage, Description = description,
            ExpectedCloseDate = expectedCloseDate,
            AssignedUserId = assignedUserId,
            Probability = GetDefaultProbability(stage)
        };
    }

    public void AdvanceStage(OpportunityStage newStage)
    {
        Stage = newStage;
        Probability = GetDefaultProbability(newStage);

        if (newStage is OpportunityStage.ClosedWon or OpportunityStage.ClosedLost)
            ActualCloseDate = DateTime.UtcNow;
    }

    public void MarkAsLost(string reason)
    {
        Stage = OpportunityStage.ClosedLost;
        LostReason = reason;
        ActualCloseDate = DateTime.UtcNow;
        Probability = 0;
    }

    private static int GetDefaultProbability(OpportunityStage stage) => stage switch
    {
        OpportunityStage.Lead => 10,
        OpportunityStage.Qualified => 25,
        OpportunityStage.Proposal => 50,
        OpportunityStage.Negotiation => 75,
        OpportunityStage.ClosedWon => 100,
        OpportunityStage.ClosedLost => 0,
        _ => 0
    };
}

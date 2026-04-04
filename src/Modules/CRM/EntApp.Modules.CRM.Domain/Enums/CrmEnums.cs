namespace EntApp.Modules.CRM.Domain.Enums;

/// <summary>Müşteri segmenti.</summary>
public enum CustomerSegment
{
    Standard = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4,
    Enterprise = 5
}

/// <summary>Müşteri tipi.</summary>
public enum CustomerType
{
    Individual = 0,
    Company = 1
}

/// <summary>Fırsat aşaması (pipeline).</summary>
public enum OpportunityStage
{
    Lead = 0,
    Qualified = 1,
    Proposal = 2,
    Negotiation = 3,
    ClosedWon = 4,
    ClosedLost = 5
}

/// <summary>Aktivite tipi.</summary>
public enum ActivityType
{
    Call = 0,
    Email = 1,
    Meeting = 2,
    Task = 3,
    Note = 4
}

/// <summary>Aktivite durumu.</summary>
public enum ActivityStatus
{
    Planned = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

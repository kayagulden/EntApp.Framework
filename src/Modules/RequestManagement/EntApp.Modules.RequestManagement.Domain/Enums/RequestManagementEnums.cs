namespace EntApp.Modules.RequestManagement.Domain.Enums;

public enum TicketStatus
{
    New = 0,
    Open = 1,
    InProgress = 2,
    WaitingForInfo = 3,
    Escalated = 4,
    Resolved = 5,
    Closed = 6,
    Cancelled = 7,
    Reopened = 8
}

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
    Urgent = 4
}

public enum TicketChannel
{
    Portal = 0,
    Email = 1,
    Phone = 2,
    Chat = 3,
    Internal = 4
}

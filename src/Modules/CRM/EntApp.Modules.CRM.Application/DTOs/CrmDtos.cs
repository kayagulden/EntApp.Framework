namespace EntApp.Modules.CRM.Application.DTOs;

public sealed record CustomerListDto(
    Guid Id, string Name, string? Code, string? Email, string? Phone,
    string? City, string? Country, string CustomerType, string Segment, DateTime CreatedAt);

public sealed record ContactListDto(
    Guid Id, Guid CustomerId, string FirstName, string LastName,
    string? Title, string? Email, string? Phone, bool IsPrimary);

public sealed record OpportunityListDto(
    Guid Id, string Title, string? CustomerName, decimal EstimatedValue,
    string Currency, string Stage, int Probability,
    DateTime? ExpectedCloseDate, DateTime CreatedAt);

public sealed record PipelineStageDto(string Stage, int Count, decimal TotalValue);

public sealed record ActivityListDto(
    Guid Id, string Subject, string ActivityType, string Status,
    Guid? CustomerId, DateTime? DueDate, DateTime CreatedAt);

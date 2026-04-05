using MediatR;

namespace EntApp.Modules.CRM.Application.Commands;

public sealed record CreateCustomerCommand(
    string Name, string? Code, string? Email, string? Phone,
    string? Address, string? City, string? Country, string? TaxNumber,
    string CustomerType = "Company", string Segment = "Standard") : IRequest<Guid>;

public sealed record CreateContactCommand(
    Guid CustomerId, string FirstName, string LastName,
    string? Title, string? Email, string? Phone,
    string? Department, bool IsPrimary = false) : IRequest<Guid>;

public sealed record CreateOpportunityCommand(
    Guid CustomerId, string Title, decimal EstimatedValue = 0,
    string? Currency = null, string Stage = "Lead",
    string? Description = null, DateTime? ExpectedCloseDate = null,
    Guid? AssignedUserId = null) : IRequest<Guid>;

public sealed record AdvanceOpportunityStageCommand(
    Guid OpportunityId, string Stage) : IRequest<AdvanceStageResult>;

public sealed record AdvanceStageResult(Guid Id, string Stage, int Probability);

public sealed record CreateActivityCommand(
    string Subject, string ActivityType = "Note", Guid? CustomerId = null,
    Guid? OpportunityId = null, string? Description = null,
    DateTime? DueDate = null, Guid? AssignedUserId = null) : IRequest<Guid>;

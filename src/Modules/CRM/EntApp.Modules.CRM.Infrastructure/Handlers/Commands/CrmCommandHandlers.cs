using EntApp.Modules.CRM.Application.Commands;
using EntApp.Modules.CRM.Application.IntegrationEvents;
using EntApp.Modules.CRM.Domain.Entities;
using EntApp.Modules.CRM.Domain.Enums;
using EntApp.Modules.CRM.Domain.Ids;
using EntApp.Modules.CRM.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.CRM.Infrastructure.Handlers.Commands;

public sealed class CreateCustomerCommandHandler(CrmDbContext db, IEventBus eventBus)
    : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        Enum.TryParse<CustomerType>(request.CustomerType, out var type);
        Enum.TryParse<CustomerSegment>(request.Segment, out var segment);
        var customer = CustomerBase.Create(request.Name, type, request.Code, request.Email,
            request.Phone, request.Address, request.City, request.Country, request.TaxNumber, segment);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new CustomerCreatedEvent(
            customer.Id.Value, customer.Name, customer.CustomerType.ToString(),
            customer.Email, customer.TaxNumber));

        return customer.Id.Value;
    }
}

public sealed class CreateContactCommandHandler(CrmDbContext db)
    : IRequestHandler<CreateContactCommand, Guid>
{
    public async Task<Guid> Handle(CreateContactCommand request, CancellationToken ct)
    {
        var contact = ContactBase.Create(new CustomerId(request.CustomerId), request.FirstName,
            request.LastName, request.Title, request.Email, request.Phone, request.Department, request.IsPrimary);
        db.Contacts.Add(contact);
        await db.SaveChangesAsync(ct);
        return contact.Id.Value;
    }
}

public sealed class CreateOpportunityCommandHandler(CrmDbContext db)
    : IRequestHandler<CreateOpportunityCommand, Guid>
{
    public async Task<Guid> Handle(CreateOpportunityCommand request, CancellationToken ct)
    {
        Enum.TryParse<OpportunityStage>(request.Stage, out var stage);
        var opp = OpportunityBase.Create(new CustomerId(request.CustomerId), request.Title,
            request.EstimatedValue, request.Currency ?? "TRY", stage, request.Description,
            request.ExpectedCloseDate, request.AssignedUserId);
        db.Opportunities.Add(opp);
        await db.SaveChangesAsync(ct);
        return opp.Id.Value;
    }
}

public sealed class AdvanceOpportunityStageCommandHandler(CrmDbContext db, IEventBus eventBus)
    : IRequestHandler<AdvanceOpportunityStageCommand, AdvanceStageResult>
{
    public async Task<AdvanceStageResult> Handle(AdvanceOpportunityStageCommand request, CancellationToken ct)
    {
        var opp = await db.Opportunities.Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id.Value == request.OpportunityId, ct)
            ?? throw new KeyNotFoundException($"Opportunity {request.OpportunityId} not found");

        if (!Enum.TryParse<OpportunityStage>(request.Stage, out var stage))
            throw new ArgumentException($"Invalid stage: {request.Stage}");

        opp.AdvanceStage(stage);
        await db.SaveChangesAsync(ct);

        if (stage == OpportunityStage.ClosedWon)
            await eventBus.PublishAsync(new OpportunityWonEvent(
                opp.Id.Value, opp.Title, opp.CustomerId.Value, opp.Customer.Name,
                opp.EstimatedValue, opp.Currency));
        else if (stage == OpportunityStage.ClosedLost)
            await eventBus.PublishAsync(new OpportunityLostEvent(
                opp.Id.Value, opp.Title, opp.CustomerId.Value, opp.LostReason));

        return new AdvanceStageResult(opp.Id.Value, opp.Stage.ToString(), opp.Probability);
    }
}

public sealed class CreateActivityCommandHandler(CrmDbContext db)
    : IRequestHandler<CreateActivityCommand, Guid>
{
    public async Task<Guid> Handle(CreateActivityCommand request, CancellationToken ct)
    {
        Enum.TryParse<ActivityType>(request.ActivityType, out var type);
        var activity = ActivityBase.Create(request.Subject, type,
            request.CustomerId.HasValue ? new CustomerId(request.CustomerId.Value) : null,
            request.OpportunityId.HasValue ? new OpportunityId(request.OpportunityId.Value) : null,
            request.Description, request.DueDate, request.AssignedUserId);
        db.Activities.Add(activity);
        await db.SaveChangesAsync(ct);
        return activity.Id.Value;
    }
}

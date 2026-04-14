using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Commands;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Ticket'a otomatik yorum ekler.
/// Workflow adımları arasında bilgi notları bırakmak için kullanılır.
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Adds a comment to a ticket.",
    DisplayName = "Add Comment")]
public sealed class AddCommentActivity : CodeActivity
{
    [Input(Description = "The ticket ID to add a comment to.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Comment content text.")]
    public Input<string> Content { get; set; } = default!;

    [Input(Description = "Whether the comment is internal (not visible to requester).")]
    public Input<bool> IsInternal { get; set; } = default!;

    [Output(Description = "The created comment ID.")]
    public Output<Guid> CommentId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);
        var content = context.Get(Content) ?? string.Empty;
        var isInternal = context.Get(IsInternal);

        var mediator = context.GetRequiredService<ISender>();
        var commentId = await mediator.Send(new AddCommentCommand(ticketId, content, isInternal));

        context.Set(CommentId, commentId);
        await context.CompleteActivityAsync();
    }
}

using EntApp.Modules.Notification.Domain.Entities;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using FluentValidation;
using MediatR;

namespace EntApp.Modules.Notification.Application.DTOs
{
    public sealed record NotificationTemplateDto(
        Guid Id, string Code, string Name, string? Description,
        string Channel, string Subject, string Body,
        string? Language, bool IsActive, Guid? TenantId);

    public sealed record NotificationLogDto(
        Guid Id, Guid? UserId, string Recipient, string Channel,
        string? TemplateCode, string Subject, string Body,
        string Status, string? ErrorMessage, DateTime SentAt, DateTime? ReadAt);

    public sealed record UserPreferenceDto(
        Guid Id, Guid UserId, string Channel, bool IsEnabled);
}

namespace EntApp.Modules.Notification.Application.Commands
{
    using DTOs;

    public sealed record CreateTemplateCommand(
        string Code, string Name, string Channel,
        string Subject, string Body,
        string? Description = null, string? Language = null, Guid? TenantId = null
    ) : IRequest<Result<Guid>>;

    public sealed record UpdateTemplateCommand(
        Guid TemplateId, string Subject, string Body
    ) : IRequest<Result>;

    public sealed record SendNotificationCommand(
        Guid? UserId, string Recipient, string Channel,
        string? TemplateCode = null,
        string? Subject = null, string? Body = null,
        Dictionary<string, object>? TemplateData = null,
        Guid? TenantId = null
    ) : IRequest<Result<Guid>>;

    public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

    public sealed record UpdatePreferenceCommand(
        Guid UserId, string Channel, bool IsEnabled, Guid? TenantId = null
    ) : IRequest<Result>;
}

namespace EntApp.Modules.Notification.Application.Queries
{
    using DTOs;

    public sealed record GetTemplatesQuery(
        string? Channel = null, Guid? TenantId = null
    ) : IRequest<Result<IReadOnlyList<NotificationTemplateDto>>>;

    public sealed record GetNotificationHistoryQuery(
        int Page = 1, int PageSize = 20,
        Guid? UserId = null, string? Channel = null,
        string? Status = null
    ) : IRequest<Result<PagedResult<NotificationLogDto>>>;

    public sealed record GetUserPreferencesQuery(
        Guid UserId, Guid? TenantId = null
    ) : IRequest<Result<IReadOnlyList<UserPreferenceDto>>>;

    public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<Result<int>>;
}

namespace EntApp.Modules.Notification.Application.Validators
{
    using Commands;

    public sealed class CreateTemplateValidator : AbstractValidator<CreateTemplateCommand>
    {
        public CreateTemplateValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Channel).NotEmpty()
                .Must(c => Enum.TryParse<NotificationChannel>(c, true, out _))
                .WithMessage("Geçersiz bildirim kanalı.");
            RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Body).NotEmpty();
        }
    }

    public sealed class SendNotificationValidator : AbstractValidator<SendNotificationCommand>
    {
        public SendNotificationValidator()
        {
            RuleFor(x => x.Recipient).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Channel).NotEmpty()
                .Must(c => Enum.TryParse<NotificationChannel>(c, true, out _))
                .WithMessage("Geçersiz bildirim kanalı.");
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.TemplateCode) ||
                           (!string.IsNullOrWhiteSpace(x.Subject) && !string.IsNullOrWhiteSpace(x.Body)))
                .WithMessage("TemplateCode veya Subject+Body belirtilmelidir.");
        }
    }
}

using EntApp.Modules.MultiTenancy.Domain.Entities;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using FluentValidation;
using MediatR;

namespace EntApp.Modules.MultiTenancy.Application.DTOs
{
    public sealed record TenantDto(
        Guid Id, string Name, string Identifier, string? DisplayName,
        string? Description, string? Subdomain, string Status, string Plan,
        string? AdminEmail, string? LogoUrl,
        DateTime? ActivatedAt, DateTime? SuspendedAt,
        IReadOnlyList<TenantSettingDto> Settings);

    public sealed record TenantSettingDto(Guid Id, string Key, string Value);
    public sealed record TenantSummaryDto(Guid Id, string Name, string Identifier, string Status, string Plan, string? AdminEmail);
}

namespace EntApp.Modules.MultiTenancy.Application.Commands
{
    public sealed record CreateTenantCommand(
        string Name, string Identifier, string? AdminEmail = null,
        string? DisplayName = null, string? Description = null, string Plan = "Free"
    ) : IRequest<Result<Guid>>;

    public sealed record UpdateTenantCommand(
        Guid TenantId, string? DisplayName, string? Description, string? LogoUrl
    ) : IRequest<Result>;

    public sealed record ActivateTenantCommand(Guid TenantId) : IRequest<Result>;
    public sealed record SuspendTenantCommand(Guid TenantId, string? Reason = null) : IRequest<Result>;
    public sealed record DeactivateTenantCommand(Guid TenantId) : IRequest<Result>;
    public sealed record ChangePlanCommand(Guid TenantId, string Plan) : IRequest<Result>;
    public sealed record SetSubdomainCommand(Guid TenantId, string Subdomain) : IRequest<Result>;

    public sealed record UpsertTenantSettingCommand(
        Guid TenantId, string Key, string Value
    ) : IRequest<Result>;
}

namespace EntApp.Modules.MultiTenancy.Application.Queries
{
    using DTOs;

    public sealed record GetTenantsQuery(
        int Page = 1, int PageSize = 20,
        string? Status = null, string? Plan = null
    ) : IRequest<Result<PagedResult<TenantSummaryDto>>>;

    public sealed record GetTenantByIdQuery(Guid TenantId) : IRequest<Result<TenantDto>>;
    public sealed record GetTenantByIdentifierQuery(string Identifier) : IRequest<Result<TenantDto>>;
}

namespace EntApp.Modules.MultiTenancy.Application.Abstractions
{
    /// <summary>
    /// Yeni tenant oluşturulduğunda modüllerin seed çalıştırması için interface.
    /// Her modül kendi ITenantSeeder implementasyonunu sağlar.
    /// </summary>
    public interface ITenantSeeder
    {
        int Order { get; }
        Task SeedAsync(Guid tenantId, CancellationToken ct = default);
    }
}

namespace EntApp.Modules.MultiTenancy.Application.Validators
{
    using Commands;

    public sealed class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
    {
        public CreateTenantValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Identifier).NotEmpty().MinimumLength(3).MaximumLength(50)
                .Matches("^[a-z0-9-]+$").WithMessage("Tanımlayıcı sadece küçük harf, rakam ve tire içerebilir.");
            RuleFor(x => x.Plan).NotEmpty().MaximumLength(50);
            RuleFor(x => x.AdminEmail).MaximumLength(300)
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.AdminEmail));
        }
    }

    public sealed class UpsertSettingValidator : AbstractValidator<UpsertTenantSettingCommand>
    {
        public UpsertSettingValidator()
        {
            RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Value).NotNull().MaximumLength(4000);
        }
    }
}

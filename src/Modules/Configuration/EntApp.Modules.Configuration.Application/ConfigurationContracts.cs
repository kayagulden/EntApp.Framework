using EntApp.Modules.Configuration.Domain.Entities;
using EntApp.Shared.Kernel.Results;
using FluentValidation;
using MediatR;

namespace EntApp.Modules.Configuration.Application.DTOs
{
    public sealed record AppSettingDto(
        Guid Id, string Key, string Value, string ValueType,
        string? Description, string? Group,
        Guid? TenantId, bool IsEncrypted, bool IsReadOnly);

    public sealed record FeatureFlagDto(
        Guid Id, string Name, string DisplayName, string? Description,
        bool IsEnabled, bool IsEffectivelyEnabled,
        Guid? TenantId, DateTime? EnabledFrom, DateTime? EnabledUntil,
        string? AllowedRoles);
}

namespace EntApp.Modules.Configuration.Application.Commands
{
    using DTOs;

    public sealed record UpsertAppSettingCommand(
        string Key, string Value, string ValueType,
        string? Description = null, string? Group = null,
        Guid? TenantId = null, bool IsEncrypted = false
    ) : IRequest<Result<Guid>>;

    public sealed record CreateFeatureFlagCommand(
        string Name, string DisplayName, string? Description = null,
        bool IsEnabled = false, Guid? TenantId = null
    ) : IRequest<Result<Guid>>;

    public sealed record ToggleFeatureFlagCommand(Guid FlagId) : IRequest<Result>;
    public sealed record SetFeatureFlagScheduleCommand(
        Guid FlagId, DateTime? EnabledFrom, DateTime? EnabledUntil) : IRequest<Result>;
}

namespace EntApp.Modules.Configuration.Application.Queries
{
    using DTOs;

    public sealed record GetAppSettingsQuery(
        string? Group = null, Guid? TenantId = null
    ) : IRequest<Result<IReadOnlyList<AppSettingDto>>>;

    public sealed record GetAppSettingByKeyQuery(
        string Key, Guid? TenantId = null
    ) : IRequest<Result<AppSettingDto>>;

    public sealed record GetFeatureFlagsQuery(
        Guid? TenantId = null
    ) : IRequest<Result<IReadOnlyList<FeatureFlagDto>>>;

    public sealed record IsFeatureEnabledQuery(
        string FlagName, Guid? TenantId = null
    ) : IRequest<Result<bool>>;
}

namespace EntApp.Modules.Configuration.Application.Validators
{
    using Commands;

    public sealed class UpsertAppSettingValidator : AbstractValidator<UpsertAppSettingCommand>
    {
        public UpsertAppSettingValidator()
        {
            RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Value).NotNull();
            RuleFor(x => x.ValueType).NotEmpty()
                .Must(v => Enum.TryParse<SettingValueType>(v, true, out _))
                .WithMessage("Geçersiz değer tipi.");
        }
    }

    public sealed class CreateFeatureFlagValidator : AbstractValidator<CreateFeatureFlagCommand>
    {
        public CreateFeatureFlagValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
        }
    }
}

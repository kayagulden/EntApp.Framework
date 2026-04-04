using EntApp.Modules.Localization.Domain.Entities;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using FluentValidation;
using MediatR;

namespace EntApp.Modules.Localization.Application.DTOs
{
    public sealed record LanguageDto(
        Guid Id, string Code, string Name, string NativeName,
        bool IsDefault, bool IsActive, int DisplayOrder);

    public sealed record TranslationDto(
        Guid Id, string LanguageCode, string Namespace, string Key,
        string Value, bool IsVerified, string FullKey, Guid? TenantId);

    public sealed record TranslationBulkDto(string LanguageCode, string Namespace, string Key, string Value);
}

namespace EntApp.Modules.Localization.Application.Commands
{
    using DTOs;

    public sealed record CreateLanguageCommand(
        string Code, string Name, string NativeName,
        bool IsDefault = false, int DisplayOrder = 0
    ) : IRequest<Result<Guid>>;

    public sealed record SetDefaultLanguageCommand(Guid LanguageId) : IRequest<Result>;

    public sealed record ToggleLanguageCommand(Guid LanguageId) : IRequest<Result>;

    public sealed record UpsertTranslationCommand(
        string LanguageCode, string Namespace, string Key, string Value,
        Guid? TenantId = null, string? ModifiedBy = null
    ) : IRequest<Result<Guid>>;

    public sealed record BulkUpsertTranslationsCommand(
        IReadOnlyList<TranslationBulkDto> Translations,
        Guid? TenantId = null, string? ModifiedBy = null
    ) : IRequest<Result<int>>;

    public sealed record VerifyTranslationCommand(Guid TranslationId) : IRequest<Result>;

    public sealed record DeleteTranslationCommand(Guid TranslationId) : IRequest<Result>;
}

namespace EntApp.Modules.Localization.Application.Queries
{
    using DTOs;

    public sealed record GetLanguagesQuery(bool ActiveOnly = true) : IRequest<Result<IReadOnlyList<LanguageDto>>>;

    public sealed record GetTranslationsQuery(
        string LanguageCode, string? Namespace = null,
        Guid? TenantId = null
    ) : IRequest<Result<IReadOnlyList<TranslationDto>>>;

    public sealed record GetTranslationsByKeyQuery(
        string Key, string? Namespace = null
    ) : IRequest<Result<IReadOnlyList<TranslationDto>>>;

    /// <summary>Frontend için: tüm namespace.key → value map (flat JSON).</summary>
    public sealed record GetTranslationMapQuery(
        string LanguageCode, string? Namespace = null, Guid? TenantId = null
    ) : IRequest<Result<Dictionary<string, string>>>;
}

namespace EntApp.Modules.Localization.Application.Validators
{
    using Commands;

    public sealed class CreateLanguageValidator : AbstractValidator<CreateLanguageCommand>
    {
        public CreateLanguageValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MinimumLength(2).MaximumLength(10)
                .Matches("^[a-z]{2}(-[a-zA-Z]{2,4})?$").WithMessage("Geçersiz dil kodu formatı (ör: tr, en-US).");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.NativeName).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class UpsertTranslationValidator : AbstractValidator<UpsertTranslationCommand>
    {
        public UpsertTranslationValidator()
        {
            RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Namespace).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Key).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Value).NotNull().MaximumLength(10000);
        }
    }
}

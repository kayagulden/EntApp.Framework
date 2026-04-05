using EntApp.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntApp.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converter: IEntityId ↔ Guid.
/// Tüm Strongly Typed ID'leri otomatik olarak Guid'e dönüştürür.
/// </summary>
public sealed class StronglyTypedIdValueConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct, IEntityId
{
    public StronglyTypedIdValueConverter()
        : base(
            id => id.Value,
            guid => (TId)Activator.CreateInstance(typeof(TId), guid)!)
    {
    }
}

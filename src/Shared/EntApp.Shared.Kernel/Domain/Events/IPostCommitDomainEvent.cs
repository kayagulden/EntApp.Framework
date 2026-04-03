using MediatR;

namespace EntApp.Shared.Kernel.Domain.Events;

/// <summary>
/// Post-commit domain event marker interface.
/// Transaction başarıyla tamamlandıktan sonra dispatch edilir.
/// Kullanım: email gönderme, bildirim, cache invalidation, loglama gibi yan etkiler.
/// </summary>
public interface IPostCommitDomainEvent : INotification
{
    /// <summary>
    /// Event'in oluştuğu zaman (UTC).
    /// </summary>
    DateTime OccurredOn { get; }
}

using EntApp.Modules.RequestManagement.Domain.Entities;
using EntApp.Modules.RequestManagement.Domain.Enums;
using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain.Entities;

namespace EntApp.Modules.RequestManagement.Infrastructure.Services;

/// <summary>
/// Ticket → Queue routing çözümleyici.
/// Waterfall sırası: ExplicitQueue → Category.DefaultQueue → Department.DefaultQueue → Unrouted.
/// </summary>
public static class TicketRoutingService
{
    /// <summary>
    /// Verilen bilgilere göre ticket'ın yönlendirileceği queue'yu ve routing kaynağını belirler.
    /// </summary>
    /// <param name="explicitQueueId">Kullanıcının açıkça seçtiği queue (null ise otomatik routing).</param>
    /// <param name="category">Ticket'ın kategorisi (DefaultQueueId kontrolü için).</param>
    /// <param name="department">Ticket'ın departmanı (DefaultQueueId kontrolü için).</param>
    /// <returns>Resolved queue ID ve routing kaynağı.</returns>
    public static (ServiceQueueId? QueueId, TicketRoutingSource Source) ResolveQueue(
        ServiceQueueId? explicitQueueId,
        RequestCategory? category,
        Department? department)
    {
        // 1. Kullanıcı açıkça queue seçtiyse → Manual
        if (explicitQueueId.HasValue)
            return (explicitQueueId, TicketRoutingSource.Manual);

        // 2. Kategori'nin default queue'su varsa → CategoryDefault
        if (category?.DefaultQueueId.HasValue == true)
            return (category.DefaultQueueId, TicketRoutingSource.CategoryDefault);

        // 3. Departman'ın default queue'su varsa → DepartmentDefault
        if (department?.DefaultQueueId.HasValue == true)
            return (new ServiceQueueId(department.DefaultQueueId.Value), TicketRoutingSource.DepartmentDefault);

        // 4. Hiçbiri eşleşmedi → Unrouted
        return (null, TicketRoutingSource.Unrouted);
    }
}

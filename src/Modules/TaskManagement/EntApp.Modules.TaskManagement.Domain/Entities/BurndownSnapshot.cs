using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Burndown snapshot — sprint içinde günlük SP durumunu kaydeder.
/// Her status değişikliğinde o günün kaydı upsert edilir.
/// </summary>
public sealed class BurndownSnapshot : BaseEntity<BurndownSnapshotId>
{
    public SprintId SprintId { get; private set; }

    /// <summary>Hangi gün (sadece tarih, saat yok).</summary>
    public DateTime Date { get; private set; }

    /// <summary>O gün kalan SP.</summary>
    public int RemainingPoints { get; private set; }

    /// <summary>O güne kadar tamamlanan SP.</summary>
    public int CompletedPoints { get; private set; }

    /// <summary>Toplam iş kalemi sayısı.</summary>
    public int TotalItems { get; private set; }

    /// <summary>Tamamlanan iş kalemi sayısı.</summary>
    public int CompletedItems { get; private set; }

    private BurndownSnapshot() { }

    public static BurndownSnapshot Create(SprintId sprintId,
        int remainingPoints, int completedPoints,
        int totalItems, int completedItems)
    {
        return new BurndownSnapshot
        {
            Id = EntityId.New<BurndownSnapshotId>(),
            SprintId = sprintId,
            Date = DateTime.UtcNow.Date,
            RemainingPoints = remainingPoints,
            CompletedPoints = completedPoints,
            TotalItems = totalItems,
            CompletedItems = completedItems
        };
    }

    /// <summary>Aynı gündeki mevcut snapshot'ı günceller.</summary>
    public void Update(int remainingPoints, int completedPoints,
        int totalItems, int completedItems)
    {
        RemainingPoints = remainingPoints;
        CompletedPoints = completedPoints;
        TotalItems = totalItems;
        CompletedItems = completedItems;
    }
}

using EntApp.Modules.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.TaskManagement.Infrastructure.Services;

/// <summary>
/// Projesiz görevler için global tenant-scoped numara üretici.
/// Projeli görevler Project.NextWorkItemNumber() kullanır, projesiz görevler bu servisi kullanır.
/// Format: TSK-00001, TSK-00002, ...
/// </summary>
public static class WorkItemNumberGenerator
{
    private const string Prefix = "TSK-";

    public static async Task<string> NextAsync(TaskManagementDbContext db, CancellationToken ct = default)
    {
        var maxNumber = await db.WorkItems
            .Where(t => t.ProjectId == null && t.WorkItemNumber.StartsWith(Prefix))
            .OrderByDescending(t => t.WorkItemNumber)
            .Select(t => t.WorkItemNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (maxNumber is not null && maxNumber.Length > Prefix.Length)
        {
            if (int.TryParse(maxNumber[Prefix.Length..], out var parsed))
                seq = parsed + 1;
        }

        return $"{Prefix}{seq:D5}";
    }
}

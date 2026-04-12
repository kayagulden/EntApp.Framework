using EntApp.Modules.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.TaskManagement.Infrastructure.Services;

/// <summary>
/// Projesiz görevler için global tenant-scoped numara üretici.
/// Projeli görevler Project.NextTaskNumber() kullanır, projesiz görevler bu servisi kullanır.
/// Format: TSK-00001, TSK-00002, ...
/// </summary>
public static class TaskNumberGenerator
{
    private const string Prefix = "TSK-";

    public static async Task<string> NextAsync(TaskManagementDbContext db, CancellationToken ct = default)
    {
        var maxNumber = await db.Tasks
            .Where(t => t.ProjectId == null && t.TaskNumber.StartsWith(Prefix))
            .OrderByDescending(t => t.TaskNumber)
            .Select(t => t.TaskNumber)
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

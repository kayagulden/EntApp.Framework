using EntApp.Modules.RequestManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.RequestManagement.Infrastructure.Services;

/// <summary>Sequential ticket number generator (REQ-0001, REQ-0002, ...).</summary>
public static class TicketNumberGenerator
{
    public static async Task<string> NextAsync(RequestManagementDbContext db, CancellationToken ct = default)
    {
        var maxNumber = await db.Tickets
            .Select(t => t.Number)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(ct);

        if (maxNumber is null) return "REQ-0001";

        // Parse: "REQ-0042" → 42 → 43 → "REQ-0043"
        var prefix = "REQ-";
        if (maxNumber.StartsWith(prefix) && int.TryParse(maxNumber[prefix.Length..], out var seq))
        {
            return $"{prefix}{(seq + 1):D4}";
        }

        return $"{prefix}0001";
    }
}

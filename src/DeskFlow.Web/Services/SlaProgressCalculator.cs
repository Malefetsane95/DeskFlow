using DeskFlow.Web.ViewModels.Staff;

namespace DeskFlow.Web.Services;

public static class SlaProgressCalculator
{
    private static readonly TimeSpan DueSoonWindow = TimeSpan.FromHours(1);

    public static SlaProgressViewModel Calculate(
        string name,
        DateTimeOffset createdAt,
        int? targetMinutes,
        DateTimeOffset? completedAt,
        DateTimeOffset now)
    {
        if (targetMinutes is null or <= 0)
        {
            return new SlaProgressViewModel
            {
                Name = name,
                State = "No target set",
                StateClass = "none",
                Summary = "No target set"
            };
        }

        var dueAt = createdAt.AddMinutes(targetMinutes.Value);
        if (completedAt is not null)
        {
            var variance = dueAt - completedAt.Value;
            var met = variance >= TimeSpan.Zero;
            var summary = variance == TimeSpan.Zero
                ? "Met on time"
                : met
                    ? $"Met · {FormatDuration(variance)} to spare"
                    : $"Late by {FormatDuration(-variance)}";

            return new SlaProgressViewModel
            {
                Name = name,
                State = met ? "Met" : "Missed",
                StateClass = met ? "met" : "late",
                Summary = summary,
                DueAt = dueAt
            };
        }

        var remaining = dueAt - now;
        if (remaining < TimeSpan.Zero)
        {
            return new SlaProgressViewModel
            {
                Name = name,
                State = "Overdue",
                StateClass = "overdue",
                Summary = $"Overdue by {FormatDuration(-remaining)}",
                DueAt = dueAt
            };
        }

        var dueSoon = remaining <= DueSoonWindow;
        return new SlaProgressViewModel
        {
            Name = name,
            State = dueSoon ? "Due soon" : "On track",
            StateClass = dueSoon ? "due-soon" : "on-track",
            Summary = $"Due in {FormatDuration(remaining)}",
            DueAt = dueAt
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes));
        var days = totalMinutes / 1440;
        var hours = totalMinutes % 1440 / 60;
        var minutes = totalMinutes % 60;

        if (days > 0) return hours > 0 ? $"{days}d {hours}h" : $"{days}d";
        if (hours > 0) return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        return $"{minutes}m";
    }
}

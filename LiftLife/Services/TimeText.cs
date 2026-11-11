namespace LiftLife.Services;

/// <summary>Friendly time labels used across the app ("2m ago", "Today 2:14 PM").</summary>
public static class TimeText
{
    public static string Relative(DateTime time)
    {
        var span = DateTime.Now - time;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return time.ToString("dd MMM");
    }

    public static string Stamp(DateTime time) =>
        time.Date == DateTime.Today ? $"Today {time:h:mm tt}"
        : time.Date == DateTime.Today.AddDays(-1) ? $"Yesterday {time:h:mm tt}"
        : time.ToString("dd MMM, h:mm tt");
}

using SQLite;

namespace LiftLife.Models;

/// <summary>
/// A piece of community gym equipment whose live status members can view and report.
/// Backs the Community Station Hub (screen 2.0) and Report Status modal (2.1).
/// </summary>
[Table("stations")]
public class Station
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Equipment category, e.g. Benches, Cables, Squat Racks.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Primary muscle group this station trains (used by the swap engine).</summary>
    public string MuscleGroup { get; set; } = string.Empty;

    public StationStatus Status { get; set; } = StationStatus.Free;

    /// <summary>Optional note left when reporting a fault or busy state.</summary>
    public string? StatusNote { get; set; }

    /// <summary>Name of the member who last reported the status (crowdsourced).</summary>
    public string? ReportedBy { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.Now;

    // ----- Convenience properties for the UI (not stored) -----

    [Ignore]
    public string StatusLabel => Status switch
    {
        StationStatus.Free => "FREE",
        StationStatus.Busy => "BUSY",
        StationStatus.OutOfOrder => "OUT OF ORDER",
        _ => "UNKNOWN"
    };

    /// <summary>Subtitle under the station name, e.g. "Occupied • Reported 2m ago by Alex".</summary>
    [Ignore]
    public string DetailLine => Status switch
    {
        StationStatus.Busy => $"Occupied • Reported {RelativeUpdated} by {ReportedBy ?? "a member"}",
        StationStatus.Free => $"Free • Updated {RelativeUpdated}",
        _ => $"{(string.IsNullOrWhiteSpace(StatusNote) ? "Under maintenance" : StatusNote)} • Reported {RelativeUpdated}"
    };

    /// <summary>Label for the main card button, which changes with the station status.</summary>
    [Ignore]
    public string PrimaryActionText => Status switch
    {
        StationStatus.Free => "Claim Station",
        StationStatus.Busy => "Station Busy? Swap",
        _ => "Find Alternative"
    };

    [Ignore]
    public string RelativeUpdated
    {
        get
        {
            var span = DateTime.Now - LastUpdated;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return LastUpdated.ToString("dd MMM");
        }
    }
}

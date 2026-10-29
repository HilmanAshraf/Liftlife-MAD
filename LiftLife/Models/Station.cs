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

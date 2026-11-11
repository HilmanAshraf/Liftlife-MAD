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

    /// <summary>True when this member claimed the station, so they can release it when done.</summary>
    public bool ClaimedByMe { get; set; }

    /// <summary>A "Busy" report older than this is probably out of date.</summary>
    public const int StaleAfterMinutes = 45;

    // ----- Convenience properties for the UI (not stored) -----

    /// <summary>Busy for a long time with no update (and not claimed by me) — may be free now.</summary>
    [Ignore]
    public bool IsStale =>
        Status == StationStatus.Busy && !ClaimedByMe &&
        (DateTime.Now - LastUpdated).TotalMinutes >= StaleAfterMinutes;

    [Ignore]
    public string StatusLabel => Status switch
    {
        StationStatus.Free => "FREE",
        StationStatus.Busy when IsStale => "BUSY?",
        StationStatus.Busy => "BUSY",
        StationStatus.OutOfOrder => "OUT OF ORDER",
        _ => "UNKNOWN"
    };

    /// <summary>Subtitle under the station name, e.g. "Occupied • Reported 2m ago by Alex".</summary>
    [Ignore]
    public string DetailLine => Status switch
    {
        StationStatus.Busy when ClaimedByMe => $"In use by you • since {RelativeUpdated}",
        StationStatus.Busy when IsStale => $"Last reported busy {RelativeUpdated} by {ReportedBy ?? "a member"} • may be free now",
        StationStatus.Busy => $"Occupied • Reported {RelativeUpdated} by {ReportedBy ?? "a member"}",
        StationStatus.Free => $"Free • Updated {RelativeUpdated}{(string.IsNullOrWhiteSpace(ReportedBy) ? "" : $" by {ReportedBy}")}",
        _ => $"{(string.IsNullOrWhiteSpace(StatusNote) ? "Under maintenance" : StatusNote)} • Reported {RelativeUpdated}"
    };

    /// <summary>Label for the main card button, which changes with the station status.</summary>
    [Ignore]
    public string PrimaryActionText => Status switch
    {
        StationStatus.Free => "Claim Station",
        StationStatus.Busy when ClaimedByMe => "I'm Done • Release",
        StationStatus.Busy => "Station Busy? Swap",
        _ => "Find Alternative"
    };

    [Ignore]
    public string RelativeUpdated => Services.TimeText.Relative(LastUpdated);
}

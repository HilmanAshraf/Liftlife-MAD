using LiftLife.Services;
using SQLite;

namespace LiftLife.Models;

/// <summary>
/// One entry in a station's maintenance log. Every status change (report, claim,
/// release) adds a row, so the history of busy periods and faults is kept.
/// </summary>
[Table("station_reports")]
public class StationReport
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int StationId { get; set; }

    public StationStatus Status { get; set; }

    public string? Note { get; set; }

    public string ReportedBy { get; set; } = string.Empty;

    public DateTime ReportedAt { get; set; } = DateTime.Now;

    // ----- Display helpers (not stored) -----

    [Ignore]
    public string StatusLabel => Status switch
    {
        StationStatus.Free => "FREE",
        StationStatus.Busy => "BUSY",
        _ => "OUT OF ORDER"
    };

    [Ignore]
    public string HeadlineText => $"{StatusLabel} • {ReportedBy}";

    [Ignore]
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    [Ignore]
    public string TimeLine => $"{TimeText.Stamp(ReportedAt)} • {TimeText.Relative(ReportedAt)}";
}

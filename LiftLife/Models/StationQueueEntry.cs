using LiftLife.Services;
using SQLite;

namespace LiftLife.Models;

/// <summary>
/// One member waiting for a busy station. The queue is first come, first served
/// (ordered by <see cref="JoinedAt"/>).
/// </summary>
[Table("station_queue")]
public class StationQueueEntry
{
    /// <summary>Queue entries older than this are treated as expired (the member probably left).</summary>
    public const int ExpireAfterMinutes = 120;

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int StationId { get; set; }

    public string MemberName { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; } = DateTime.Now;

    /// <summary>True for this phone's member, so they can leave the queue.</summary>
    public bool IsMe { get; set; }

    // ----- Display helpers (not stored) -----

    [Ignore] public int Position { get; set; }

    [Ignore]
    public string DisplayName => IsMe ? $"{MemberName} (you)" : MemberName;

    [Ignore]
    public string JoinedText => $"joined {TimeText.Relative(JoinedAt)}";
}

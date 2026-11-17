using System.Collections.ObjectModel;
using LiftLife.Data;
using LiftLife.Models;

namespace LiftLife.ViewModels;

/// <summary>
/// ViewModel for a station's detail page: its live status and full maintenance log
/// (every busy / free / out-of-order report, newest first).
/// </summary>
public class StationDetailViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private Station? _station;
    private string _summaryText = string.Empty;

    public StationDetailViewModel(LiftLifeRepository repo) => _repo = repo;

    public ObservableCollection<StationReport> Reports { get; } = new();

    /// <summary>Members waiting for this station, in order.</summary>
    public ObservableCollection<StationQueueEntry> Queue { get; } = new();

    public bool HasQueue => Queue.Count > 0;

    public string QueueHeader => Queue.Count == 0
        ? "QUEUE • nobody waiting"
        : $"QUEUE • {Queue.Count} waiting • about {Queue.Count * Station.MinutesPerMember} min total";

    public async Task ToggleQueueAsync(string memberName)
    {
        if (Station is null) return;
        if (Station.IsInMyQueue)
            await _repo.LeaveQueueAsync(Station.Id);
        else
            await _repo.JoinQueueAsync(Station.Id, memberName);
        await LoadAsync(Station.Id);
    }

    public Station? Station
    {
        get => _station;
        private set
        {
            if (SetProperty(ref _station, value))
                OnPropertyChanged(nameof(TagLine));
        }
    }

    public string TagLine => Station is null ? string.Empty : $"{Station.Category} • trains {Station.MuscleGroup}";

    /// <summary>e.g. "7 reports in the last 30 days • 2 fault reports".</summary>
    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public async Task LoadAsync(int stationId)
    {
        Station = await _repo.GetStationAsync(stationId);

        var reports = await _repo.GetStationReportsAsync(stationId);
        Reports.Clear();
        foreach (var report in reports)
            Reports.Add(report);

        var recent = reports.Where(r => r.ReportedAt >= DateTime.Now.AddDays(-30)).ToList();
        var faults = recent.Count(r => r.Status == StationStatus.OutOfOrder);
        SummaryText = $"{recent.Count} report{(recent.Count == 1 ? "" : "s")} in the last 30 days • " +
                      $"{faults} fault report{(faults == 1 ? "" : "s")}";

        Queue.Clear();
        foreach (var entry in await _repo.GetQueueAsync(stationId))
            Queue.Add(entry);
        OnPropertyChanged(nameof(HasQueue));
        OnPropertyChanged(nameof(QueueHeader));
    }
}

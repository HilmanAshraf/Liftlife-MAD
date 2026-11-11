using LiftLife.Data;
using LiftLife.Models;
using LiftLife.Services;

namespace LiftLife.Views;

/// <summary>
/// Report Status modal (screen 2.1). The member picks Available / Busy / Out of Order,
/// optionally adds a fault note, and submits. The new status is saved to SQLite.
/// </summary>
public partial class ReportStatusPage : ContentPage
{
    private readonly Station _station;
    private readonly string _reportedBy;
    private readonly Func<Task> _onSaved;
    private readonly LiftLifeRepository _repo;
    private StationStatus _selected;

    /// <param name="station">The station being reported.</param>
    /// <param name="reportedBy">The member's name, saved on the report.</param>
    /// <param name="onSaved">Called after saving so the previous page can refresh.</param>
    public ReportStatusPage(Station station, string reportedBy, Func<Task> onSaved)
    {
        InitializeComponent();
        _station = station;
        _reportedBy = reportedBy;
        _onSaved = onSaved;
        _repo = ServiceHelper.Get<LiftLifeRepository>();

        StationNameLabel.Text = station.Name;
        NoteEditor.Text = station.Status == StationStatus.OutOfOrder ? station.StatusNote : string.Empty;
        Select(station.Status); // start with the current status highlighted
    }

    private void OnFreeTapped(object? sender, TappedEventArgs e) => Select(StationStatus.Free);
    private void OnBusyTapped(object? sender, TappedEventArgs e) => Select(StationStatus.Busy);
    private void OnBrokenTapped(object? sender, TappedEventArgs e) => Select(StationStatus.OutOfOrder);

    /// <summary>Highlights the chosen option with a lime border.</summary>
    private void Select(StationStatus status)
    {
        _selected = status;
        var lime = (Color)Application.Current!.Resources["Primary"];
        var normal = (Color)Application.Current.Resources["StrokeColor"];

        FreeOption.Stroke = status == StationStatus.Free ? lime : normal;
        BusyOption.Stroke = status == StationStatus.Busy ? lime : normal;
        BrokenOption.Stroke = status == StationStatus.OutOfOrder ? lime : normal;

        FreeOption.StrokeThickness = status == StationStatus.Free ? 2 : 1;
        BusyOption.StrokeThickness = status == StationStatus.Busy ? 2 : 1;
        BrokenOption.StrokeThickness = status == StationStatus.OutOfOrder ? 2 : 1;
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        var note = NoteEditor.Text?.Trim();

        // Validation: a broken machine report must say what is wrong.
        if (_selected == StationStatus.OutOfOrder && string.IsNullOrWhiteSpace(note))
        {
            await DisplayAlertAsync("Add a Note", "Please describe the fault so staff know what to fix.", "OK");
            return;
        }

        var savedNote = _selected switch
        {
            StationStatus.Busy => string.IsNullOrWhiteSpace(note) ? "Occupied" : note,
            StationStatus.OutOfOrder => note,
            _ => null
        };

        await _repo.UpdateStationStatusAsync(_station.Id, _selected, savedNote, _reportedBy);

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        await _onSaved();
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync(); // dismiss without saving
}

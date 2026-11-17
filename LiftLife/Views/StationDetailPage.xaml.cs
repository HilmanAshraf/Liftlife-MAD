using LiftLife.Data;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>Station detail + maintenance log, opened by tapping a card on the Community Hub.</summary>
public partial class StationDetailPage : ContentPage
{
    private readonly StationDetailViewModel _vm;
    private readonly LiftLifeRepository _repo;
    private readonly int _stationId;

    public StationDetailPage(int stationId)
    {
        InitializeComponent();
        _stationId = stationId;
        _vm = ServiceHelper.Get<StationDetailViewModel>();
        _repo = ServiceHelper.Get<LiftLifeRepository>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync(_stationId);
    }

    private async void OnQueueClicked(object? sender, EventArgs e)
    {
        if (_vm.Station is null) return;

        var name = await MemberNamePrompt.EnsureAsync(this, _repo);
        if (name is null) return;

        await _vm.ToggleQueueAsync(name);
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private async void OnUpdateStatusClicked(object? sender, EventArgs e)
    {
        if (_vm.Station is null) return;

        var name = await MemberNamePrompt.EnsureAsync(this, _repo);
        if (name is null) return;

        // The new report appears at the top of the log after saving.
        await Navigation.PushModalAsync(new ReportStatusPage(_vm.Station, name, () => _vm.LoadAsync(_stationId)));
    }
}

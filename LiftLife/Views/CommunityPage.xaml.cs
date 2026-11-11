using LiftLife.Data;
using LiftLife.Models;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>
/// Community Station Hub (screen 2.0). The ViewModel holds the data; this code-behind
/// handles user actions that need dialogs or navigation.
/// </summary>
public partial class CommunityPage : ContentPage
{
    private readonly CommunityViewModel _vm;
    private readonly LiftLifeRepository _repo;

    public CommunityPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.Get<CommunityViewModel>();
        _repo = ServiceHelper.Get<LiftLifeRepository>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnChipTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is CategoryChip chip)
            await _vm.SelectCategoryAsync(chip);
    }

    /// <summary>Tap a station card to open its maintenance log.</summary>
    private async void OnStationTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is Station station)
            await Navigation.PushAsync(new StationDetailPage(station.Id));
    }

    /// <summary>
    /// The main card button changes with status:
    /// Free = claim it, Busy (mine) = release it, Busy / Out of Order = suggest a swap.
    /// </summary>
    private async void OnPrimaryActionClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Station station)
            return;

        if (station.Status == StationStatus.Free)
        {
            var name = await MemberNamePrompt.EnsureAsync(this, _repo);
            if (name is null) return;

            var confirm = await DisplayAlertAsync("Claim Station",
                $"Mark {station.Name} as in use by you?", "Claim", "Cancel");
            if (!confirm) return;

            await _vm.ClaimAsync(station, name);
            Vibrate();
            return;
        }

        if (station.Status == StationStatus.Busy && station.ClaimedByMe)
        {
            var name = await MemberNamePrompt.EnsureAsync(this, _repo);
            if (name is null) return;

            await _vm.ReleaseAsync(station, name);
            Vibrate();
            await DisplayAlertAsync("Station Released", $"{station.Name} is now free for the next member. Thanks!", "OK");
            return;
        }

        await ShowSwapOptionsAsync(station);
    }

    /// <summary>Runs the swap engine for the station's muscle group and shows the results.</summary>
    private async Task ShowSwapOptionsAsync(Station station)
    {
        var options = await _vm.GetAlternativesAsync(station);
        if (options.Count == 0)
        {
            await DisplayAlertAsync("No Alternatives",
                $"No other {station.MuscleGroup.ToLower()} exercises found.", "OK");
            return;
        }

        var labels = options
            .Select(o => $"{o.Exercise.Name} ({o.Exercise.Equipment}) • {o.AvailabilityLabel}")
            .ToArray();

        var choice = await DisplayActionSheetAsync(
            $"Same muscle group: {station.MuscleGroup}", "Cancel", null, labels);

        var index = Array.IndexOf(labels, choice);
        if (index < 0) return;

        var picked = options[index];
        Vibrate();
        await DisplayAlertAsync("Swap Suggested",
            $"Do {picked.Exercise.Name} instead.\n\nForm cue: {picked.Exercise.FormCue}", "Got it");
    }

    private async void OnUpdateStatusClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Station station)
            return;

        var name = await MemberNamePrompt.EnsureAsync(this, _repo);
        if (name is null) return;

        // Open the Report Status modal; reload the list after it saves.
        await Navigation.PushModalAsync(new ReportStatusPage(station, name, _vm.LoadAsync));
    }

    /// <summary>Short haptic tap as feedback (MAUI HapticFeedback API).</summary>
    private static void Vibrate()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { /* haptics not supported on this device/simulator */ }
    }
}

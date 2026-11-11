using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>
/// Active Workout Session (screen 3.1). The ViewModel does the logic; this page handles
/// dialogs, navigation, gestures and device APIs (keep screen on, haptics).
/// </summary>
public partial class ActiveWorkoutPage : ContentPage
{
    private readonly ActiveWorkoutViewModel _vm;
    private readonly int _routineId;
    private bool _loaded;

    public ActiveWorkoutPage(int routineId)
    {
        InitializeComponent();
        _routineId = routineId;
        _vm = ServiceHelper.Get<ActiveWorkoutViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Keep the phone awake while training (MAUI DeviceDisplay API).
        DeviceDisplay.Current.KeepScreenOn = true;

        if (!_loaded)
        {
            _loaded = true;
            await _vm.LoadAsync(_routineId);
        }
        _vm.StartClock();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;

        // Opening the swap / form guide sheets also triggers this, but the page is still
        // in the stack then — only stop the clock when the page has really been closed.
        if (!Navigation.NavigationStack.Contains(this))
            _vm.StopClock();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        var discard = await DisplayAlertAsync("Discard Workout?",
            "Nothing from this session will be saved.", "Discard", "Keep Training");
        if (!discard) return;

        _vm.StopClock();
        await Navigation.PopAsync();
    }

    private async void OnFinishClicked(object? sender, EventArgs e) => await FinishAsync();

    private async Task FinishAsync()
    {
        var unfinished = _vm.UnfinishedSetCount;
        if (unfinished > 0)
        {
            var finish = await DisplayAlertAsync("Finish Workout?",
                $"You still have {unfinished} unticked set{(unfinished == 1 ? "" : "s")}. They won't be saved.",
                "Finish", "Keep Training");
            if (!finish) return;
        }

        var (summary, error) = await _vm.FinishAsync();
        if (summary is null)
        {
            await DisplayAlertAsync("Nothing Logged", error, "OK");
            return;
        }

        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }

        // Show the summary in place of this page, so Back goes to the Routine Hub.
        await Navigation.PushAsync(new WorkoutSummaryPage(summary));
        Navigation.RemovePage(this);
    }

    private async void OnDoneTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not SessionSet set)
            return;

        var error = _vm.ToggleDone(set);
        if (error is not null)
        {
            await DisplayAlertAsync("Check This Set", error, "OK");
            return;
        }

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private void OnAddSetClicked(object? sender, EventArgs e) => _vm.AddSet();
    private void OnAddRestClicked(object? sender, EventArgs e) => _vm.AddRestTime(15);
    private void OnSkipRestClicked(object? sender, EventArgs e) => _vm.SkipRest();
    private void OnPreviousClicked(object? sender, EventArgs e) => _vm.Previous();
    private void OnSwipedLeft(object? sender, SwipedEventArgs e) => _vm.Next();
    private void OnSwipedRight(object? sender, SwipedEventArgs e) => _vm.Previous();

    private async void OnNextClicked(object? sender, EventArgs e)
    {
        if (_vm.IsLastExercise)
            await FinishAsync();
        else
            _vm.Next();
    }

    private async void OnFormGuideClicked(object? sender, EventArgs e)
    {
        if (_vm.CurrentExercise is not null)
            await Navigation.PushModalAsync(new FormGuidePage(_vm.CurrentExercise));
    }

    private async void OnSwapClicked(object? sender, EventArgs e)
    {
        if (_vm.CurrentExercise is null) return;
        await Navigation.PushModalAsync(new QuickSwapPage(_vm.CurrentExercise, _vm.ApplySwapAsync));
    }
}

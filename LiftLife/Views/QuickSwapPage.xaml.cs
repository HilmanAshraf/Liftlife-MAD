using LiftLife.Data;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>Quick Swap sheet (screen 4.3), opened from "Station Busy? Swap" during a workout.</summary>
public partial class QuickSwapPage : ContentPage
{
    private readonly QuickSwapViewModel _vm;
    private readonly SessionExercise _current;
    private readonly Func<ExerciseSwapOption, bool, Task> _onConfirm;
    private bool _loaded;

    /// <param name="current">The exercise being replaced.</param>
    /// <param name="onConfirm">Applies the swap: (chosen option, update routine permanently).</param>
    public QuickSwapPage(SessionExercise current, Func<ExerciseSwapOption, bool, Task> onConfirm)
    {
        InitializeComponent();
        _current = current;
        _onConfirm = onConfirm;
        _vm = ServiceHelper.Get<QuickSwapViewModel>();
        BindingContext = _vm;
        ReplacingLabel.Text = $"Replacing {current.Name}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        await _vm.LoadAsync(_current);
    }

    private void OnChoiceTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is SwapChoice choice)
            _vm.Select(choice);
    }

    private async void OnConfirmClicked(object? sender, EventArgs e)
    {
        var selected = _vm.Selected;
        if (selected is null)
        {
            await DisplayAlertAsync("Pick a Substitute", "Tap one of the exercises first.", "OK");
            return;
        }

        // Completed sets and the session timer are kept; only remaining sets move over.
        await _onConfirm(selected.Option, _vm.UpdateRoutine);
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}

using LiftLife.Models;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>Workout Routine Hub (screen 3.0).</summary>
public partial class WorkoutsPage : ContentPage
{
    private readonly WorkoutsViewModel _vm;

    public WorkoutsPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.Get<WorkoutsViewModel>();
        BindingContext = _vm;
    }

    // Runs on first show and again when returning from the Routine Builder.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnNewClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new RoutineBuilderPage(routineId: 0));

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is Routine routine)
            await Navigation.PushAsync(new RoutineBuilderPage(routine.Id));
    }

    private async void OnDeleteInvoked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Routine routine)
            return;

        var confirm = await DisplayAlertAsync("Delete Routine",
            $"Delete \"{routine.Name}\"? This can't be undone.", "Delete", "Cancel");
        if (confirm)
            await _vm.DeleteAsync(routine);
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Routine routine)
            return;

        if (routine.ExerciseCount == 0)
        {
            await DisplayAlertAsync("Empty Routine", "Add at least one exercise before starting.", "OK");
            return;
        }

        await Navigation.PushAsync(new ActiveWorkoutPage(routine.Id));
    }
}

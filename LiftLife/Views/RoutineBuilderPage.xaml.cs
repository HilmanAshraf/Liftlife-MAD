using LiftLife.Models;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>Routine Builder (screen 4.1): create or edit a workout routine.</summary>
public partial class RoutineBuilderPage : ContentPage
{
    private readonly RoutineBuilderViewModel _vm;
    private readonly int _routineId;
    private bool _loaded;

    /// <param name="routineId">0 to create a new routine, otherwise the routine to edit.</param>
    public RoutineBuilderPage(int routineId)
    {
        InitializeComponent();
        _routineId = routineId;
        _vm = ServiceHelper.Get<RoutineBuilderViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return; // only load once, so unsaved edits aren't overwritten
        _loaded = true;
        await _vm.LoadAsync(_routineId);
    }

    /// <summary>Two-step picker: choose a muscle group, then an exercise in it.</summary>
    private async void OnAddExerciseClicked(object? sender, EventArgs e)
    {
        var groups = _vm.Catalogue.Select(x => x.MuscleGroup).Distinct().ToArray();
        var group = await DisplayActionSheetAsync("Muscle group", "Cancel", null, groups);
        if (group is null || group == "Cancel") return;

        var exercises = _vm.Catalogue.Where(x => x.MuscleGroup == group).ToList();
        var names = exercises.Select(x => $"{x.Name} ({x.Equipment})").ToArray();
        var picked = await DisplayActionSheetAsync(group, "Cancel", null, names);
        if (picked is null || picked == "Cancel") return;

        var exercise = exercises[Array.IndexOf(names, picked)];
        _vm.AddExercise(exercise);
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RoutineExercise item)
            _vm.Remove(item);
    }

    private void OnMoveUpClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RoutineExercise item)
            _vm.MoveUp(item);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var error = await _vm.SaveAsync();
        if (error is not null)
        {
            await DisplayAlertAsync("Can't Save Yet", error, "OK");
            return;
        }

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Navigation.PopAsync(); // back to the Routine Hub, which reloads
    }
}

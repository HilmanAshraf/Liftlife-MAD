using System.Collections.ObjectModel;
using LiftLife.Data;
using LiftLife.Models;

namespace LiftLife.ViewModels;

/// <summary>
/// ViewModel for the Routine Builder (screen 4.1). Creates a new routine or edits an
/// existing one: name, focus, estimated time, and the ordered list of exercises.
/// </summary>
public class RoutineBuilderViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private Routine _routine = new();
    private string _name = string.Empty;
    private string _focus = string.Empty;
    private string _estimatedMinutes = "45";
    private string _pageTitle = "New Routine";

    public RoutineBuilderViewModel(LiftLifeRepository repo) => _repo = repo;

    public ObservableCollection<RoutineExercise> Items { get; } = new();

    /// <summary>The full exercise catalogue, used by the "Add Exercise" picker.</summary>
    public List<Exercise> Catalogue { get; private set; } = new();

    public string PageTitle
    {
        get => _pageTitle;
        set => SetProperty(ref _pageTitle, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Focus
    {
        get => _focus;
        set => SetProperty(ref _focus, value);
    }

    /// <summary>Kept as text so the Entry can be validated before saving.</summary>
    public string EstimatedMinutes
    {
        get => _estimatedMinutes;
        set => SetProperty(ref _estimatedMinutes, value);
    }

    public string ExerciseHeader => $"EXERCISES ({Items.Count})";

    /// <param name="routineId">0 = create a new routine.</param>
    public async Task LoadAsync(int routineId)
    {
        Catalogue = await _repo.GetExercisesAsync();

        if (routineId == 0)
        {
            _routine = new Routine();
            PageTitle = "New Routine";
            return;
        }

        _routine = await _repo.GetRoutineAsync(routineId) ?? new Routine();
        PageTitle = "Edit Routine";
        Name = _routine.Name;
        Focus = _routine.Focus;
        EstimatedMinutes = _routine.EstimatedMinutes.ToString();

        Items.Clear();
        foreach (var item in await _repo.GetRoutineExercisesAsync(routineId))
            Items.Add(item);
        OnPropertyChanged(nameof(ExerciseHeader));
    }

    public void AddExercise(Exercise exercise)
    {
        Items.Add(new RoutineExercise
        {
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            MuscleGroup = exercise.MuscleGroup,
            Equipment = exercise.Equipment,
            FormCue = exercise.FormCue,
            RestSeconds = exercise.DefaultRestSeconds,
            OrderIndex = Items.Count
        });
        OnPropertyChanged(nameof(ExerciseHeader));
    }

    public void Remove(RoutineExercise item)
    {
        Items.Remove(item);
        Renumber();
    }

    /// <summary>Moves an exercise one place up the list.</summary>
    public void MoveUp(RoutineExercise item)
    {
        var index = Items.IndexOf(item);
        if (index <= 0) return;
        Items.Move(index, index - 1);
        Renumber();
    }

    /// <summary>
    /// Refreshes the 1, 2, 3... numbers. Items don't notify changes themselves,
    /// so the list is rebuilt to redraw them.
    /// </summary>
    private void Renumber()
    {
        var copy = Items.ToList();
        Items.Clear();
        for (var i = 0; i < copy.Count; i++)
        {
            copy[i].OrderIndex = i;
            Items.Add(copy[i]);
        }
        OnPropertyChanged(nameof(ExerciseHeader));
    }

    /// <summary>Validates input, then saves the routine and its exercises to SQLite.</summary>
    /// <returns>An error message, or null if saved successfully.</returns>
    public async Task<string?> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Please give the routine a name.";

        if (!int.TryParse(EstimatedMinutes, out var minutes) || minutes is < 5 or > 240)
            return "Estimated time must be between 5 and 240 minutes.";

        if (Items.Count == 0)
            return "Add at least one exercise.";

        if (Items.Any(i => i.TargetSets is < 1 or > 10 || i.TargetReps is < 1 or > 50 || i.RestSeconds is < 0 or > 600))
            return "Check each exercise: sets 1–10, reps 1–50, rest 0–600 seconds.";

        _routine.Name = Name.Trim();
        _routine.Focus = Focus.Trim();
        _routine.EstimatedMinutes = minutes;

        var id = await _repo.SaveRoutineAsync(_routine);
        await _repo.ReplaceRoutineExercisesAsync(id, Items.ToList());
        return null;
    }
}

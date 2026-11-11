using System.Collections.ObjectModel;
using LiftLife.Data;

namespace LiftLife.ViewModels;

/// <summary>One substitute option on the Quick Swap sheet.</summary>
public class SwapChoice : BaseViewModel
{
    private bool _isSelected;

    public ExerciseSwapOption Option { get; init; } = new();
    public string Name => Option.Exercise.Name;
    public string Detail { get; init; } = string.Empty;
    public string AvailabilityText => Option.IsEquipmentFree ? "● Free in Gym" : "● Check availability";
    public bool IsFree => Option.IsEquipmentFree;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

/// <summary>
/// ViewModel for the Quick Swap sheet (screen 4.3): biomechanical substitutes for the
/// current exercise, each with the member's personal record and live equipment status.
/// </summary>
public class QuickSwapViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private string _targetText = string.Empty;
    private bool _updateRoutine;
    private bool _isEmpty;

    public QuickSwapViewModel(LiftLifeRepository repo) => _repo = repo;

    public ObservableCollection<SwapChoice> Choices { get; } = new();

    public string TargetText
    {
        get => _targetText;
        private set => SetProperty(ref _targetText, value);
    }

    /// <summary>Scope selector: off = this session only, on = change the routine permanently.</summary>
    public bool UpdateRoutine
    {
        get => _updateRoutine;
        set => SetProperty(ref _updateRoutine, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public SwapChoice? Selected => Choices.FirstOrDefault(c => c.IsSelected);

    public async Task LoadAsync(SessionExercise current)
    {
        TargetText = $"Target: {current.MuscleGroup} (Same Mechanics)";

        Choices.Clear();
        foreach (var option in await _repo.GetSwapOptionsAsync(current.ExerciseId))
        {
            var pr = await _repo.GetPersonalRecordAsync(option.Exercise.Id);
            var prText = pr is null ? "No PR yet" : $"Your PR: {pr.WeightKg:0.#} kg × {pr.Reps} reps";
            Choices.Add(new SwapChoice
            {
                Option = option,
                Detail = $"{option.Exercise.Equipment} • {prText}"
            });
        }

        // Pre-select the best option (available equipment first, already sorted).
        if (Choices.Count > 0)
            Choices[0].IsSelected = true;

        IsEmpty = Choices.Count == 0;
    }

    public void Select(SwapChoice choice)
    {
        foreach (var c in Choices)
            c.IsSelected = c == choice;
    }
}

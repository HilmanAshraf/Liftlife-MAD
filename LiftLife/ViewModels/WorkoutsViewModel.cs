using System.Collections.ObjectModel;
using LiftLife.Data;
using LiftLife.Models;

namespace LiftLife.ViewModels;

/// <summary>
/// ViewModel for the Workout Routine Hub (screen 3.0): the saved routines and the
/// most recent completed session (volume history).
/// </summary>
public class WorkoutsViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private string _historyText = "No workouts logged yet";
    private bool _isRefreshing;

    public WorkoutsViewModel(LiftLifeRepository repo)
    {
        _repo = repo;
        RefreshCommand = new Command(async () =>
        {
            await LoadAsync();
            IsRefreshing = false;
        });
    }

    public ObservableCollection<Routine> Routines { get; } = new();

    public Command RefreshCommand { get; }

    public string HistoryText
    {
        get => _historyText;
        set => SetProperty(ref _historyText, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public async Task LoadAsync()
    {
        var routines = await _repo.GetRoutinesAsync();
        Routines.Clear();
        foreach (var r in routines)
            Routines.Add(r);

        var last = await _repo.GetLastCompletedSessionAsync();
        HistoryText = last?.HistoryText ?? "No workouts logged yet";
    }

    public async Task DeleteAsync(Routine routine)
    {
        await _repo.DeleteRoutineAsync(routine.Id);
        Routines.Remove(routine);
    }
}

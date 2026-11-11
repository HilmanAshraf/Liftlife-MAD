using System.Collections.ObjectModel;
using System.Globalization;
using LiftLife.Data;
using LiftLife.Models;

namespace LiftLife.ViewModels;

/// <summary>One row in the set table: SET | PREV | KG | REPS | DONE.</summary>
public class SessionSet : BaseViewModel
{
    private string _weight = string.Empty;
    private string _reps = string.Empty;
    private string _prevText = "—";
    private bool _isDone;

    public int SetNumber { get; init; }

    /// <summary>Which exercise this set is logged under (stays fixed once the set is done).</summary>
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;

    public string PrevText
    {
        get => _prevText;
        set => SetProperty(ref _prevText, value);
    }

    /// <summary>Kept as text so the Entry can be validated before ticking Done.</summary>
    public string Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    public string Reps
    {
        get => _reps;
        set => SetProperty(ref _reps, value);
    }

    public bool IsDone
    {
        get => _isDone;
        set
        {
            if (SetProperty(ref _isDone, value))
                OnPropertyChanged(nameof(IsNotDone));
        }
    }

    /// <summary>Inputs are locked once a set is ticked; untick to edit.</summary>
    public bool IsNotDone => !IsDone;

    public double WeightKg => ActiveWorkoutViewModel.ParseWeight(Weight) ?? 0;
    public int RepsCount => int.TryParse(Reps, out var r) ? r : 0;
}

/// <summary>An exercise in the live session, with its own list of sets.</summary>
public class SessionExercise : BaseViewModel
{
    private int _exerciseId;
    private string _name = string.Empty;
    private string _equipment = string.Empty;
    private string _swappedFrom = string.Empty;

    public int RoutineExerciseId { get; init; }
    public string MuscleGroup { get; init; } = string.Empty;
    public string FormCue { get; set; } = string.Empty;
    public string SetupNote { get; init; } = string.Empty;
    public int RestSeconds { get; set; } = 90;
    public int TargetReps { get; init; } = 10;

    /// <summary>Last session's sets for this exercise (for PREV and pre-filling).</summary>
    public List<SetLog> PreviousSets { get; set; } = new();

    public ObservableCollection<SessionSet> Sets { get; } = new();

    public int ExerciseId
    {
        get => _exerciseId;
        set => SetProperty(ref _exerciseId, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Equipment
    {
        get => _equipment;
        set
        {
            if (SetProperty(ref _equipment, value))
                OnPropertyChanged(nameof(TagLine));
        }
    }

    public string SwappedFrom
    {
        get => _swappedFrom;
        set
        {
            if (SetProperty(ref _swappedFrom, value))
            {
                OnPropertyChanged(nameof(HasSwap));
                OnPropertyChanged(nameof(SwapText));
            }
        }
    }

    public string TagLine => $"{MuscleGroup} • {Equipment}";
    public bool HasSwap => !string.IsNullOrEmpty(SwappedFrom);
    public string SwapText => $"⇄ Swapped from {SwappedFrom}";
    public bool HasSetupNote => !string.IsNullOrWhiteSpace(SetupNote);
    public string SetupNoteText => $"📌 {SetupNote}";

    /// <summary>"PREV" text for a given set number, e.g. "80kg × 6".</summary>
    public string PrevFor(int setNumber)
    {
        var prev = PreviousSets.FirstOrDefault(p => p.SetNumber == setNumber);
        return prev is null ? "—" : $"{prev.WeightKg:0.#}kg × {prev.Reps}";
    }
}

/// <summary>
/// ViewModel for the Active Workout Session (screen 3.1). Tracks the elapsed time,
/// the rest countdown, the logged sets and total volume, and saves everything on Finish.
/// </summary>
public class ActiveWorkoutViewModel : BaseViewModel
{
    private const int RestDoneHoldSeconds = 3;

    private readonly LiftLifeRepository _repo;
    private IDispatcherTimer? _timer;
    private DateTime _startedAt;
    private int _routineId;
    private int _currentIndex;
    private SessionExercise? _currentExercise;
    private string _routineName = "Workout";
    private string _elapsedText = "00:00";
    private string _volumeText = "0 kg";
    private bool _isResting;
    private int _restRemaining;
    private int _restDoneHold;

    public ActiveWorkoutViewModel(LiftLifeRepository repo) => _repo = repo;

    public ObservableCollection<SessionExercise> Exercises { get; } = new();

    public string RoutineName
    {
        get => _routineName;
        set => SetProperty(ref _routineName, value);
    }

    public SessionExercise? CurrentExercise
    {
        get => _currentExercise;
        private set => SetProperty(ref _currentExercise, value);
    }

    public string ElapsedText
    {
        get => _elapsedText;
        private set => SetProperty(ref _elapsedText, value);
    }

    public string VolumeText
    {
        get => _volumeText;
        private set => SetProperty(ref _volumeText, value);
    }

    public bool IsResting
    {
        get => _isResting;
        private set => SetProperty(ref _isResting, value);
    }

    public string RestText => _restRemaining > 0
        ? $"⏱ Rest: {TimeSpan.FromSeconds(_restRemaining):mm\\:ss}"
        : "✅ Rest over — next set!";

    public string ExerciseTitle => CurrentExercise is null
        ? string.Empty
        : $"{_currentIndex + 1}/{Exercises.Count}: {CurrentExercise.Name}";

    public string ProgressText
    {
        get
        {
            var all = Exercises.SelectMany(e => e.Sets).ToList();
            return $"EXERCISE {_currentIndex + 1} OF {Exercises.Count}  •  {all.Count(s => s.IsDone)}/{all.Count} SETS DONE";
        }
    }

    public double ProgressValue
    {
        get
        {
            var all = Exercises.SelectMany(e => e.Sets).ToList();
            return all.Count == 0 ? 0 : (double)all.Count(s => s.IsDone) / all.Count;
        }
    }

    public bool CanGoPrevious => _currentIndex > 0;
    public bool IsLastExercise => _currentIndex >= Exercises.Count - 1;
    public string NextButtonText => IsLastExercise ? "Finish Workout" : "Next Exercise →";

    public int UnfinishedSetCount => Exercises.SelectMany(e => e.Sets).Count(s => !s.IsDone);

    // ---------------- Loading ----------------

    public async Task LoadAsync(int routineId)
    {
        _routineId = routineId;
        var routine = await _repo.GetRoutineAsync(routineId);
        RoutineName = routine?.Name ?? "Workout";

        Exercises.Clear();
        foreach (var item in await _repo.GetRoutineExercisesAsync(routineId))
        {
            var exercise = new SessionExercise
            {
                RoutineExerciseId = item.Id,
                ExerciseId = item.ExerciseId,
                Name = item.ExerciseName,
                MuscleGroup = item.MuscleGroup,
                Equipment = item.Equipment,
                FormCue = item.FormCue,
                SetupNote = item.SetupNote ?? string.Empty,
                RestSeconds = item.RestSeconds,
                TargetReps = item.TargetReps,
                PreviousSets = await _repo.GetPreviousSetsAsync(item.ExerciseId)
            };

            for (var n = 1; n <= Math.Max(1, item.TargetSets); n++)
                exercise.Sets.Add(CreateSet(exercise, n));

            Exercises.Add(exercise);
        }

        _startedAt = DateTime.Now;
        _currentIndex = 0;
        RefreshCurrent();
    }

    /// <summary>New set row, pre-filled from last session (weight) and the routine target (reps).</summary>
    private static SessionSet CreateSet(SessionExercise exercise, int setNumber)
    {
        var prev = exercise.PreviousSets.FirstOrDefault(p => p.SetNumber == setNumber)
                   ?? exercise.PreviousSets.LastOrDefault();
        return new SessionSet
        {
            SetNumber = setNumber,
            ExerciseId = exercise.ExerciseId,
            ExerciseName = exercise.Name,
            PrevText = exercise.PrevFor(setNumber),
            Weight = prev is null ? string.Empty : prev.WeightKg.ToString("0.#", CultureInfo.InvariantCulture),
            Reps = exercise.TargetReps.ToString()
        };
    }

    // ---------------- Clock & rest timer ----------------

    public void StartClock()
    {
        if (_timer is not null) return;
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        Tick();
    }

    public void StopClock()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void Tick()
    {
        var elapsed = DateTime.Now - _startedAt;
        ElapsedText = elapsed.TotalHours >= 1 ? elapsed.ToString(@"h\:mm\:ss") : elapsed.ToString(@"mm\:ss");

        if (!IsResting) return;

        if (_restRemaining > 0)
        {
            _restRemaining--;
            if (_restRemaining == 0)
            {
                // Rest finished: vibrate + haptic so the user knows without looking.
                try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500)); } catch { }
                try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
                _restDoneHold = RestDoneHoldSeconds;
            }
        }
        else if (--_restDoneHold <= 0)
        {
            IsResting = false;
        }

        OnPropertyChanged(nameof(RestText));
    }

    private void StartRest(int seconds)
    {
        _restRemaining = Math.Max(5, seconds);
        IsResting = true;
        OnPropertyChanged(nameof(RestText));
    }

    public void AddRestTime(int seconds)
    {
        if (!IsResting) return;
        _restRemaining += seconds;
        OnPropertyChanged(nameof(RestText));
    }

    public void SkipRest()
    {
        _restRemaining = 0;
        IsResting = false;
    }

    // ---------------- Sets ----------------

    /// <summary>Ticks / unticks a set. Ticking validates input and starts the rest timer.</summary>
    /// <returns>An error message, or null on success.</returns>
    public string? ToggleDone(SessionSet set)
    {
        if (set.IsDone)
        {
            set.IsDone = false;
            RefreshTotals();
            return null;
        }

        var weight = ParseWeight(set.Weight);
        if (weight is null or < 0 or > 500)
            return "Enter a weight between 0 and 500 kg (use 0 for bodyweight).";

        if (!int.TryParse(set.Reps, out var reps) || reps is < 1 or > 100)
            return "Enter reps between 1 and 100.";

        set.IsDone = true;
        RefreshTotals();
        StartRest(CurrentExercise?.RestSeconds ?? 90);
        return null;
    }

    public void AddSet()
    {
        if (CurrentExercise is null) return;
        var last = CurrentExercise.Sets.LastOrDefault();
        var set = CreateSet(CurrentExercise, CurrentExercise.Sets.Count + 1);
        if (last is not null)
        {
            set.Weight = last.Weight; // copy the previous row, most people repeat the weight
            set.Reps = last.Reps;
        }
        CurrentExercise.Sets.Add(set);
        RefreshTotals();
    }

    // ---------------- Navigation between exercises ----------------

    public void Next()
    {
        if (IsLastExercise) return;
        _currentIndex++;
        RefreshCurrent();
    }

    public void Previous()
    {
        if (!CanGoPrevious) return;
        _currentIndex--;
        RefreshCurrent();
    }

    private void RefreshCurrent()
    {
        CurrentExercise = Exercises.Count == 0 ? null : Exercises[_currentIndex];
        OnPropertyChanged(nameof(ExerciseTitle));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(IsLastExercise));
        OnPropertyChanged(nameof(NextButtonText));
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        var volume = Exercises.SelectMany(e => e.Sets).Where(s => s.IsDone).Sum(s => s.WeightKg * s.RepsCount);
        VolumeText = $"{volume:N0} kg";
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressValue));
    }

    // ---------------- Swap ----------------

    /// <summary>
    /// Replaces the current exercise with a substitute. Completed sets stay logged under
    /// the original exercise; remaining sets move to the substitute. The clock keeps running.
    /// </summary>
    public async Task ApplySwapAsync(ExerciseSwapOption option, bool updateRoutine)
    {
        var current = CurrentExercise;
        if (current is null) return;

        var original = current.HasSwap ? current.SwappedFrom : current.Name;
        var substitute = option.Exercise;

        current.ExerciseId = substitute.Id;
        current.Name = substitute.Name;
        current.Equipment = substitute.Equipment;
        current.FormCue = substitute.FormCue;
        current.RestSeconds = substitute.DefaultRestSeconds;
        current.PreviousSets = await _repo.GetPreviousSetsAsync(substitute.Id);
        current.SwappedFrom = original == substitute.Name ? string.Empty : original;

        foreach (var set in current.Sets.Where(s => !s.IsDone))
        {
            var prev = current.PreviousSets.FirstOrDefault(p => p.SetNumber == set.SetNumber)
                       ?? current.PreviousSets.LastOrDefault();
            set.ExerciseId = substitute.Id;
            set.ExerciseName = substitute.Name;
            set.PrevText = current.PrevFor(set.SetNumber);
            set.Weight = prev is null ? string.Empty : prev.WeightKg.ToString("0.#", CultureInfo.InvariantCulture);
        }

        if (updateRoutine && current.RoutineExerciseId > 0)
            await _repo.UpdateRoutineExerciseAsync(current.RoutineExerciseId, substitute.Id);

        OnPropertyChanged(nameof(ExerciseTitle));
    }

    // ---------------- Finish ----------------

    /// <summary>Saves the session and its sets to SQLite, then builds the summary.</summary>
    /// <returns>The summary, or an error message if nothing was logged.</returns>
    public async Task<(WorkoutSummary? summary, string? error)> FinishAsync()
    {
        var doneSets = Exercises.SelectMany(e => e.Sets).Where(s => s.IsDone).ToList();
        if (doneSets.Count == 0)
            return (null, "Complete at least one set before finishing.");

        StopClock();
        IsResting = false;

        // Work out personal records BEFORE saving, so we compare against older history only.
        var results = new List<ExerciseResult>();
        foreach (var group in doneSets.GroupBy(s => (s.ExerciseId, s.ExerciseName)))
        {
            var best = group.OrderByDescending(s => s.WeightKg).ThenByDescending(s => s.RepsCount).First();
            var oldPr = await _repo.GetPersonalRecordAsync(group.Key.ExerciseId);
            var isNewPr = oldPr is not null &&
                          (best.WeightKg > oldPr.WeightKg ||
                           (best.WeightKg == oldPr.WeightKg && best.RepsCount > oldPr.Reps));

            results.Add(new ExerciseResult
            {
                Name = group.Key.ExerciseName,
                SetsDone = group.Count(),
                BestWeightKg = best.WeightKg,
                BestReps = best.RepsCount,
                IsNewPr = isNewPr
            });
        }

        var session = new WorkoutSession
        {
            RoutineId = _routineId,
            RoutineName = RoutineName,
            StartedAt = _startedAt
        };
        await _repo.StartSessionAsync(session);

        var logs = doneSets.Select(s => new SetLog
        {
            ExerciseId = s.ExerciseId,
            ExerciseName = s.ExerciseName,
            SetNumber = s.SetNumber,
            WeightKg = s.WeightKg,
            Reps = s.RepsCount,
            IsDone = true
        }).ToList();
        await _repo.CompleteSessionAsync(session, logs);

        var macros = await _repo.ActivateTrainingDayAsync();

        var summary = new WorkoutSummary
        {
            RoutineName = RoutineName,
            CompletedAt = session.CompletedAt ?? DateTime.Now,
            Duration = (session.CompletedAt ?? DateTime.Now) - _startedAt,
            TotalVolumeKg = session.TotalVolumeKg,
            SetsCompleted = doneSets.Count,
            Results = results,
            Macros = macros
        };
        return (summary, null);
    }

    /// <summary>Accepts "80", "80.5" or "80,5".</summary>
    public static double? ParseWeight(string? text) =>
        double.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var w)
            ? w
            : null;
}

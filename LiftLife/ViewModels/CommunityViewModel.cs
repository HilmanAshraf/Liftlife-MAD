using System.Collections.ObjectModel;
using LiftLife.Data;
using LiftLife.Models;

namespace LiftLife.ViewModels;

/// <summary>A filter chip (All, Benches, Cables...). IsSelected drives the lime highlight.</summary>
public class CategoryChip : BaseViewModel
{
    private bool _isSelected;

    public string Name { get; init; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

/// <summary>
/// ViewModel for the Community Station Hub (screen 2.0). Loads stations from SQLite,
/// handles category filtering, and calculates the live crowd-density banner.
/// </summary>
public class CommunityViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private string _selectedCategory = "All";
    private string _densityTitle = "GYM DENSITY";
    private string _densitySubtitle = string.Empty;
    private double _densityProgress;
    private bool _isRefreshing;

    public CommunityViewModel(LiftLifeRepository repo)
    {
        _repo = repo;
        // Pull-to-refresh (gesture) runs this command.
        RefreshCommand = new Command(async () =>
        {
            await LoadAsync();
            IsRefreshing = false;
        });
    }

    public ObservableCollection<Station> Stations { get; } = new();
    public ObservableCollection<CategoryChip> Categories { get; } = new();

    public Command RefreshCommand { get; }

    public string DensityTitle
    {
        get => _densityTitle;
        set => SetProperty(ref _densityTitle, value);
    }

    public string DensitySubtitle
    {
        get => _densitySubtitle;
        set => SetProperty(ref _densitySubtitle, value);
    }

    /// <summary>0.0 to 1.0, bound to the ProgressBar in the density banner.</summary>
    public double DensityProgress
    {
        get => _densityProgress;
        set => SetProperty(ref _densityProgress, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    /// <summary>Reloads chips, stations and the density banner from the database.</summary>
    public async Task LoadAsync()
    {
        if (Categories.Count == 0)
        {
            foreach (var name in await _repo.GetStationCategoriesAsync())
                Categories.Add(new CategoryChip { Name = name, IsSelected = name == _selectedCategory });
        }

        var stations = await _repo.GetStationsAsync(_selectedCategory);
        Stations.Clear();
        foreach (var s in stations)
            Stations.Add(s);

        var (percent, label, busy, usable) = await _repo.GetCrowdDensityAsync();
        DensityTitle = $"GYM DENSITY: {label} ({percent}%)";
        DensitySubtitle = $"{busy} of {usable} working stations in use right now";
        DensityProgress = percent / 100.0;
    }

    public async Task SelectCategoryAsync(CategoryChip chip)
    {
        foreach (var c in Categories)
            c.IsSelected = c == chip;

        _selectedCategory = chip.Name;
        await LoadAsync();
    }

    /// <summary>Marks a free station as busy, claimed by this member.</summary>
    public async Task ClaimAsync(Station station, string memberName)
    {
        await _repo.UpdateStationStatusAsync(station.Id, StationStatus.Busy, "In use", memberName, claimedByMe: true);
        await LoadAsync();
    }

    /// <summary>"I'm Done": frees a station this member claimed, so others can use it.</summary>
    public async Task ReleaseAsync(Station station, string memberName)
    {
        await _repo.UpdateStationStatusAsync(station.Id, StationStatus.Free, "Released", memberName);
        await LoadAsync();
    }

    public Task<List<ExerciseSwapOption>> GetAlternativesAsync(Station station) =>
        _repo.GetAlternativesForStationAsync(station);
}

using System.Collections.ObjectModel;
using LiftLife.Data;

namespace LiftLife.ViewModels;

/// <summary>One "label: value" row in the storage card.</summary>
public class StatRow
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel for the About LiftLife page (screens 5.0 / 6.1): app version and device info
/// (MAUI AppInfo / DeviceInfo APIs), the member profile, and live SQLite storage stats.
/// </summary>
public class AboutViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private string _memberNameText = "Not set yet";
    private string _storageText = string.Empty;

    public AboutViewModel(LiftLifeRepository repo) => _repo = repo;

    /// <summary>Read from the app package (ApplicationDisplayVersion / ApplicationVersion in the .csproj).</summary>
    public string VersionText => $"Version {AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";

    public string DeviceText
    {
        get
        {
            var d = DeviceInfo.Current;
            var simulator = d.DeviceType == DeviceType.Virtual ? " (Simulator)" : string.Empty;
            return $"Running on {d.Platform} {d.VersionString} • {d.Idiom}{simulator}";
        }
    }

    public string MemberNameText
    {
        get => _memberNameText;
        private set => SetProperty(ref _memberNameText, value);
    }

    /// <summary>e.g. "liftlife.db3 • 96 KB on this device".</summary>
    public string StorageText
    {
        get => _storageText;
        private set => SetProperty(ref _storageText, value);
    }

    public ObservableCollection<StatRow> Stats { get; } = new();

    public async Task<string?> GetMemberNameAsync() => await _repo.GetMemberNameAsync();

    public async Task LoadAsync()
    {
        var name = await _repo.GetMemberNameAsync();
        MemberNameText = string.IsNullOrWhiteSpace(name) ? "Not set yet" : name;

        Stats.Clear();
        foreach (var (label, count) in await _repo.GetStorageStatsAsync())
            Stats.Add(new StatRow { Label = label, Value = count.ToString("N0") });

        var file = new FileInfo(LiftLifeDatabase.DatabasePath);
        var size = file.Exists ? $"{Math.Max(1, file.Length / 1024):N0} KB" : "not created yet";
        StorageText = $"{LiftLifeDatabase.DatabaseFilename} • {size} on this device";
    }

    public async Task ResetAsync()
    {
        await _repo.ResetAllDataAsync();
        await LoadAsync();
    }
}

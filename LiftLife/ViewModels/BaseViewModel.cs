using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LiftLife.ViewModels;

/// <summary>
/// Base class for all ViewModels. Implements INotifyPropertyChanged so that when a
/// property changes, any XAML element bound to it updates automatically (data binding).
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets a backing field and raises PropertyChanged only if the value actually changed.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Demo.ViewModels;

/// <summary>
/// The smallest change-notification base that does the job, so the sample carries no MVVM
/// framework a reader would have to know before they can read it.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Raise([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Raise(propertyName);

        return true;
    }
}

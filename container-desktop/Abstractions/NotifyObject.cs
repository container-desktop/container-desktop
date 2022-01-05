using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ContainerDesktop.Abstractions;

public abstract class NotifyObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected bool SetValueAndNotify<T>(ref T store, T value, [CallerMemberName] string propertyName = null)
    {
        if (!Equals(store, value))
        {
            store = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }
        return false;
    }

    protected bool SetValueAndNotify<TValue>(Func<TValue> getter, Action<TValue> setter, TValue newValue, [CallerMemberName] string propertyName = null)
    {
        if (!Equals(getter(), newValue))
        {
            setter(newValue);
            NotifyPropertyChanged(propertyName);
            return true;
        }
        return false;
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged != null)
        {
            OnPropertyChanged(propertyName);
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
    }
}

namespace ContainerDesktop.Common;

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

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

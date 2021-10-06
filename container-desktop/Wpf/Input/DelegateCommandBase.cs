using System.Windows.Input;

namespace ContainerDesktop.UI.Wpf.Input;

public abstract class DelegateCommandBase : ICommand
{
    private readonly SynchronizationContext _synchronizationContext;

    protected DelegateCommandBase()
    {
        _synchronizationContext = SynchronizationContext.Current;
    }

    public event EventHandler CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        OnExecuteChanged();
    }

    protected virtual void OnExecuteChanged()
    {
        var handler = CanExecuteChanged;
        if (handler != null)
        {
            if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
            {
                _synchronizationContext.Post(_ => handler.Invoke(this, EventArgs.Empty), null);
            }
            else
            {
                handler.Invoke(this, EventArgs.Empty);
            }
        }
    }

    bool ICommand.CanExecute(object parameter)
    {
        return CanExecute(parameter);
    }

    void ICommand.Execute(object parameter)
    {
        Execute(parameter);
    }

    protected abstract void Execute(object parameter);
    protected abstract bool CanExecute(object parameter);
}


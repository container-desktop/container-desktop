namespace ContainerDesktop.Common.Input;

using System.Windows.Input;

public class DelegateCommand : ICommand
{
    private readonly Func<bool> _canExecute;
    private readonly Action<object> _execute;

    public DelegateCommand(Action<object> execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged()
    {
        if (CanExecuteChanged != null)
        {
            CanExecuteChanged(this, EventArgs.Empty);
        }
    }
}

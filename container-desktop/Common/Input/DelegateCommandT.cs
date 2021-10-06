namespace ContainerDesktop.Common.Input;

public class DelegateCommand<T> : DelegateCommandBase
{
    private readonly Action<T> _executeMethod;
    private readonly Func<T, bool> _canExecuteMethod;

    public DelegateCommand(Action<T> executeMethod) : this(executeMethod, _ => true) { }

    public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod) : base()
    {
        _executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
        _canExecuteMethod = canExecuteMethod ?? throw new ArgumentNullException(nameof(canExecuteMethod));
    }

    public void Execute(T parameter) => _executeMethod(parameter);
    
    public bool CanExecute(T parameter) => _canExecuteMethod(parameter);
    
    protected override void Execute(object parameter) => Execute((T)parameter);
    
    protected override bool CanExecute(object parameter) => CanExecute((T)parameter);
}

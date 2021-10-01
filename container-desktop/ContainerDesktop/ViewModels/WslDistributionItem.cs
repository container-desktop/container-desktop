using ContainerDesktop.Common.UI;

namespace ContainerDesktop.ViewModels;

public class WslDistributionItem : ViewModelBase
{
    private string _name;
    private bool _enabled;

    public string Name
    {
        get => _name;
        set => SetValueAndNotify(ref _name, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetValueAndNotify(ref _enabled, value);
    }
}

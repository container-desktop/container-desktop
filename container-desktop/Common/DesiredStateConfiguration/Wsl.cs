using ContainerDesktop.Common.Services;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public class Wsl : ResourceBase
    {
        private readonly IWslService _wslService;

        public Wsl(IWslService wslService)
        {
            _wslService = wslService;
        }

        public override void Set(ConfigurationContext context)
        {
            if (context.Uninstall)
            {
                //TODO:
                //_wslService.Disable();
            }
            else
            {
                _wslService.InstallWsl();
            }
        }

        public override bool Test(ConfigurationContext context)
        {
            var enabled = _wslService.IsWslInstalled();
            if(context.Uninstall)
            {
                enabled = !enabled;
            }
            return enabled;
        }
    }
}

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

        public override bool NeedsElevation => true;

        public override void Set(ConfigurationContext context)
        {
            _wslService.Enable();
        }

        public override bool Test(ConfigurationContext context)
        {
            return _wslService.IsEnabled();
        }
    }
}

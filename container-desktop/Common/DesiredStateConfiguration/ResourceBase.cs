using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public abstract class ResourceBase : IResource
    {
        public string Id { get; set; }

        public List<string> DependsOn { get; } = new List<string>();

        public bool Enabled { get; set; } = true;

        public virtual bool NeedsElevation { get; } = false;
        public abstract void Set(ConfigurationContext context);

        public abstract bool Test(ConfigurationContext context);
    }
}

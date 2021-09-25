namespace ContainerDesktop.Common.DesiredStateConfiguration;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class ConfigurationManifest : IConfigurationManifest
{
    public ConfigurationManifest(IServiceProvider serviceProvider, Stream content, Uri location)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var json = reader.ReadToEnd();
        var settings = CreateSerializerSettings(serviceProvider);
        JsonConvert.PopulateObject(json, this, settings);
        Location = location;
    }

    public List<IResource> Resources { get; } = new List<IResource>();

    public List<string> InstallerRestartArguments { get; } = new List<string>();

    public Uri Location { get; }

    public void Apply(ConfigurationContext context)
    {
        var graph = BuildDependencyGraph(context.Uninstall);
        var changes = graph.Count;
        context.ReportProgress(0, changes, "Start applying resources");
        Apply(graph, context);
        
        void Apply(IEnumerable<IResource> resources, ConfigurationContext ctx, int initialCount = 0, int countModifier = 1)
        {
            var prefix = ctx.Uninstall ? "Undoing" : "Applying";
            var count = initialCount;
            var processedResources = new Stack<IResource>();
            try
            {
                foreach (var resource in resources)
                {
                    if (!resource.Test(context))
                    {
                        context.ReportProgress(count, changes, $"{prefix}: {resource.Description}", resource.ExtraInformation);
                        resource.Set(context);
                        processedResources.Push(resource);
                        //TODO: on uninstall to a pending restart if needed
                        if (!context.Uninstall && resource.RequiresReboot &&
                            !(context.AskUserConsent("You need to restart your computer before continuing the installation. Do you want to restart now ?", "Reboot required") && RebootHelper.RequestReboot(true, InstallerRestartArguments)))
                        {
                            context.ReportProgress(0, changes, "Please restart your computer and run the installer again to continue the installation.");
                            return;
                        }
                    }
                    count += countModifier;
                }
            }
            catch
            {
                if (!ctx.Uninstall)
                {
                    Apply(processedResources, ctx.WithUninstall(true), count, -1);
                }
                throw;
            }
            context.ReportProgress(Math.Max(0, countModifier * changes), changes, $"Finished {prefix.ToLowerInvariant()} resources");
        }
    }

    private List<IResource> BuildDependencyGraph(bool uninstall)
    {
        var allwaysFirst = Resources.Where(x => x.RunAllwaysFirst);
        var rest = Resources.Except(allwaysFirst).ToList();

        var resolvedGraph = new List<IResource>();
        var count = -1;
        while (rest.Count > 0 && resolvedGraph.Count != count)
        {
            count = resolvedGraph.Count;
            for (var i = 0; i < rest.Count; i++)
            {
                if (rest[i].Enabled)
                {
                    for (var j = 0; j < rest[i].DependsOn.Count; j++)
                    {
                        if (resolvedGraph.Any(x => x.Id.Equals(rest[i].DependsOn[j], StringComparison.OrdinalIgnoreCase)))
                        {
                            rest[i].DependsOn.RemoveAt(j);
                        }
                    }
                    if (rest[i].DependsOn.Count == 0)
                    {
                        resolvedGraph.Add(rest[i]);
                        rest.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
        if (rest.Count > 0)
        {
            throw new ResourceException("Could not resolve dependencies.");
        }
        if (uninstall)
        {
            resolvedGraph.Reverse();
        }
        resolvedGraph.InsertRange(0, allwaysFirst);
        if(uninstall)
        {
            resolvedGraph.RemoveAll(x => x.NoUninstall);
        }
        return resolvedGraph;
    }

    private static JsonSerializerSettings CreateSerializerSettings(IServiceProvider serviceProvider)
    {
        return new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Converters = { new SubTypeJsonConverter<IResource>(a => a.Type, t => IResource.GetTypeName(t), serviceProvider) }
        };
    }
}

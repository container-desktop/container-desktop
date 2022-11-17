namespace ContainerDesktop.DesiredStateConfiguration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public ConfigurationResult Apply(ConfigurationContext context)
    {
        var graph = BuildDependencyGraph(context);
        var changes = graph.Count;
        context.ReportProgress(0, changes, "Start applying resources");
        var ret = Apply(graph, context);
        if (!context.RestartPending)
        {
            context.ClearState();
        }
        return ret;

        ConfigurationResult Apply(IEnumerable<IResource> resources, ConfigurationContext ctx, int initialCount = 0, int countModifier = 1)
        {
            var prefix = ctx.Uninstall ? "Undoing" : "Applying";
            var count = initialCount;
            var processedResources = new Stack<IResource>();
            IResource currentResource = null;
            try
            {
                foreach (var resource in resources)
                {
                    currentResource = resource;
                    var test = resource.Test(context);
                    if(resource.RunAllways)
                    {
                        test = false;
                    }
                    else if(context.Uninstall)
                    {
                        test = !test;
                    }
                    else if(context.Updating)
                    {
                        switch(resource.OnUpdateAction)
                        {
                            case UpdateAction.Install:
                                if(test)
                                {
                                    resource.Unset(context);
                                    test = false;
                                }
                                break;
                            case UpdateAction.Skip:
                                test = true;
                                break;
                        }
                    }
                    if(!test)
                    {
                        context.ReportProgress(count, changes, $"{prefix}: {resource.Description}", resource.ExtraInformation);
                        if (context.Uninstall)
                        {
                            resource.Unset(context);
                        }
                        else
                        {
                            resource.Set(context);
                        }
                        processedResources.Push(resource);
                        //TODO: on uninstall to a pending restart if needed
                        if (resource.RequiresReboot)
                        {
                            var args = Environment.GetCommandLineArgs().Union(InstallerRestartArguments);
                            if (!(context.AskUserConsent("You need to restart your computer before continuing the installation. Do you want to restart now ?", "Reboot required") && !context.DelayReboot && RebootHelper.RequestReboot(true, args)))
                            {
                                context.ReportProgress(0, changes, "Please restart your computer and run the installer again to continue the installation.");
                            }
                            context.RestartPending = true;
                            return ConfigurationResult.PendingRestart;
                        }
                    }
                    count += countModifier;
                }
            }
            catch(Exception ex)
            {
                context.Logger?.LogError(ex, $"Failed to {prefix.ToLowerInvariant()} at [{{ResourceId}}] {{ResourceDescription}}: {{Message}}", currentResource?.Id, currentResource?.Description, ex.Message);
                if (!ctx.Uninstall)
                {
                    Apply(processedResources, ctx.WithUninstall(true), count, -1);
                    return ConfigurationResult.RolledBack;
                }
                return ConfigurationResult.Failed;
            }
            context.ReportProgress(Math.Max(0, countModifier * changes), changes, $"Finished {prefix.ToLowerInvariant()} resources");
            return ConfigurationResult.Succeeded;
        }
    }

    private List<IResource> BuildDependencyGraph(ConfigurationContext context)
    {
        var disabledResourceIdsFromState = GetDisabledResourceIdsFromState(context);
        var resources = Resources.Where(x => x.Enabled).Where(x => !disabledResourceIdsFromState.Any(y => y == x.Id));
        var disabledResources = Resources.Except(resources);
        var allwaysFirst = resources.Where(x => x.RunAllwaysFirst);
        var rest = resources.Except(allwaysFirst).ToList();

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
        if (context.Uninstall)
        {
            resolvedGraph.Reverse();
        }
        resolvedGraph.InsertRange(0, allwaysFirst);
        if(context.Uninstall)
        {
            resolvedGraph.RemoveAll(x => x.NoUninstall);
        }
        SaveDisabledResourceIdsToState(disabledResources, context);
        return resolvedGraph;
    }

    private static IEnumerable<string> GetDisabledResourceIdsFromState(ConfigurationContext context)
    {
        string[] ret = null;
        if(context.State.TryGetValue("DisabledResources", out var value) && value is JArray a)
        {
            ret = a.Values<string>().ToArray();
        }
        return ret ?? Array.Empty<string>();
    }

    private static void SaveDisabledResourceIdsToState(IEnumerable<IResource> resources, ConfigurationContext context)
    {
        var ids = resources.Where(x => !x.Enabled).Select(x => x.Id).ToArray();
        context.State["DisabledResources"] = ids;
        context.SaveState();
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

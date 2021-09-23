using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
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

        public Uri Location { get; }

        public void Apply(ConfigurationContext context)
        {
            var graph = BuildDependencyGraph(context.Uninstall);
            var changes = graph.Count(x => !x.Test(context));
            var prefix = context.Uninstall ? "Undoing" : "Applying";
            if (changes > 0)
            {
                context.ReportProgress(0, changes, "Start applying resources");
                var count = 0;
                foreach (var resource in graph)
                {
                    if (!resource.Test(context))
                    {
                         
                        context.ReportProgress(count, changes, $"{prefix}: {resource.Description}");
                        resource.Set(context);
                        //TODO: on uninstall to a pending restart if needed
                        if(!context.Uninstall && resource.RequiresReboot && 
                            !(context.AskUserConsent("You need to restart your computer before continuing the installation. Do you want to restart now ?") && RebootHelper.RequestReboot()))
                        {
                            context.ReportProgress(0, changes, "Please restart your computer and run the installer again to continue the installation.");
                            return;
                        }
                        count++;
                    }
                }
                context.ReportProgress(changes, changes, $"Finished {prefix.ToLowerInvariant()} resources");
            }
            else
            {
                context.ReportProgress(changes, changes, "No changes detected.");
            }
        }

        private List<IResource> BuildDependencyGraph(bool uninstall)
        {
            var resources = Resources.ToList();
            var resolvedGraph = new List<IResource>();
            var count = -1;
            while (resources.Count > 0 && resolvedGraph.Count != count)
            {
                count = resolvedGraph.Count;
                for (var i = 0; i < resources.Count; i++)
                {
                    if (resources[i].Enabled)
                    {
                        for (var j = 0; j < resources[i].DependsOn.Count; j++)
                        {
                            if (resolvedGraph.Any(x => x.Id.Equals(resources[i].DependsOn[j], StringComparison.OrdinalIgnoreCase)))
                            {
                                resources[i].DependsOn.RemoveAt(j);
                            }
                        }
                        if (resources[i].DependsOn.Count == 0)
                        {
                            resolvedGraph.Add(resources[i]);
                            resources.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            if (resources.Count > 0)
            {
                throw new ResourceException("Could not resolve dependencies.");
            }
            if(uninstall)
            {
                resolvedGraph.Reverse();
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
}

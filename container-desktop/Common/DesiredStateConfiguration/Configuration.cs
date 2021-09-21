using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public class Configuration
    {
        public List<IResource> Resources { get; } = new List<IResource>();

        public bool IsElevated { get; private set; }

        public string FileName { get; private set; }
        private IServiceProvider ServiceProvider { get; set; }

        public static Configuration Create(IServiceProvider serviceProvider, string fileName, bool isElevated = false)
        {
            var json = File.ReadAllText(fileName);
            var settings = CreateSerializerSettings(serviceProvider);
            var configuration = JsonConvert.DeserializeObject<Configuration>(json, settings);
            configuration.IsElevated = isElevated;
            configuration.ServiceProvider = serviceProvider;
            configuration.FileName = fileName;
            return configuration;
        }

        public void Apply(ConfigurationContext context)
        {
            var graph = BuildDependencyGraph();
            var changed = graph.Any(x => !x.Test(context));
            if (changed)
            {
                if(!context.AskUserConsent())
                {
                    throw new OperationCanceledException("Configuration is aborted by the user.");
                }
                var needsElevation = graph.Any(x => x.NeedsElevation && !x.Test(context));

                if (needsElevation && !IsElevated)
                {
                    var processExecutor = ServiceProvider.GetRequiredService<IProcessExecutor>();
                    var args = new ArgumentBuilder()
                        .Add("-c", "configuration-manifest.json")
                        .Build();
                    var ret = processExecutor.Execute("InstallerCli.exe", args, stdOut: s => context.Logger.LogInformation(s), stdErr: s => context.Logger.LogError(s));
                    if (ret != 0)
                    {
                        throw new ResourceException("Could not apply the configuration with elevated priviledges.");
                    }
                    return;
                }

                foreach (var resource in graph)
                {
                    if (!resource.Test(context))
                    {
                        resource.Set(context);
                    }
                }
            }
        }

        private List<IResource> BuildDependencyGraph()
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
                        }
                    }
                }
            }
            if (resources.Count > 0)
            {
                throw new ResourceException("Could not resolve dependencies.");
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

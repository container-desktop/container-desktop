using System;
using System.IO;
using System.Reflection;

namespace ContainerDesktop.Common
{
    public static class Product
    {
        public static string Name { get; } = "ContainerDesktop";
        public static string DisplayName { get; } = "Container Desktop";
        public static string Version { get; } = GetVersion();
        public static string InstallDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Name);
        public static string AppPath { get; } = Path.Combine(InstallDir, $"{Name}.exe");
        
        private static string GetVersion()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}

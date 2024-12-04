using System;
using System.Reflection;

namespace CrimsonSQL.Utility;

public static class AssemblyResolver
{
    public static void Resolve()
    {
        var assembly = Assembly.GetExecutingAssembly();
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            Plugin.LogInstance.LogDebug($"Attempting to resolve: {args.Name}");

            string resourceName = args.Name switch
            {
                string name when name.StartsWith("MySql.Data") => "CrimsonSQL.MySql.Data.dll",
                string name when name.StartsWith("System.Diagnostics.DiagnosticSource") => "CrimsonSQL._8._0._1.lib.net6._0.System.Diagnostics.DiagnosticSource.dll",
                string name when name.StartsWith("System.Security.Permissions") => "CrimsonSQL.System.Security.Permissions.dll",
                string name when name.StartsWith("System.Configuration.ConfigurationManager") => "CrimsonSQL.System.Configuration.ConfigurationManager.dll",
                string name when name.StartsWith("System.Text.Encoding.CodePages") => "CrimsonSQL.System.Text.Encoding.CodePages.dll",
                _ => null
            };

            if (resourceName != null)
            {
                Plugin.LogInstance.LogDebug($"Resource name mapped to: {resourceName}");
                using var stream = assembly.GetManifestResourceStream(resourceName);
                Plugin.LogInstance.LogDebug($"Stream found: {stream != null}");
                if (stream != null)
                {
                    var assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            }
            return null;
        };
    }
}

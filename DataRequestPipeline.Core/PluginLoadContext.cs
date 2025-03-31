using System.Reflection;
using System.Runtime.Loader;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginPath;

    public PluginLoadContext(string pluginPath) : base(isCollectible: false)
    {
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        // Check if the requested assembly is already loaded in the default context.
        Assembly assembly = Default.Assemblies.FirstOrDefault(a => a.FullName == assemblyName.FullName);
        if (assembly != null)
        {
            return assembly;
        }

        // For your contracts assembly, force the host version from the base directory.
        if (assemblyName.Name.Equals("DataRequestPipeline.DataContracts", StringComparison.OrdinalIgnoreCase))
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string contractsPath = Path.Combine(basePath, "DataRequestPipeline.DataContracts.dll");
            if (File.Exists(contractsPath))
            {
                return Assembly.LoadFrom(contractsPath);
            }
        }

        // Use the resolver to find the path for other assemblies.
        string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // First, try to resolve using the plugin's own dependency resolver.
        string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (!string.IsNullOrEmpty(libraryPath))
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        // If not found, try loading from the host's base directory.
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // On Windows, the native DLL name typically ends with ".dll"
        string candidate = Path.Combine(baseDir, unmanagedDllName + ".dll");
        if (File.Exists(candidate))
        {
            return LoadUnmanagedDllFromPath(candidate);
        }

        // Otherwise, fallback.
        return base.LoadUnmanagedDll(unmanagedDllName);
    }
}

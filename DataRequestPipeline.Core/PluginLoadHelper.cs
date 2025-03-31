using System.Reflection;

public static class PluginLoaderHelper
{
    //public static T LoadPlugin<T>(string pluginPath, string typeName)
    //{
    //    // Ensure the pluginPath is absolute.
    //    pluginPath = Path.GetFullPath(pluginPath);

    //    // Create the plugin loader and share the contract type T.
    //    var loader = PluginLoader.CreateFromAssemblyFile(
    //        pluginPath,
    //        sharedTypes: new[] { typeof(T) }
    //    );

    //    // Load the default assembly from the plugin.
    //    Assembly pluginAssembly = loader.LoadDefaultAssembly();

    //    // Get the plugin type.
    //    Type pluginType = pluginAssembly.GetType(typeName, throwOnError: true);

    //    // Create an instance and cast it to T.
    //    object pluginInstance = Activator.CreateInstance(pluginType);
    //    if (!(pluginInstance is T result))
    //    {
    //        throw new InvalidOperationException($"Unable to cast plugin instance to type {typeof(T).FullName}");
    //    }
    //    return result;
    //}

    public static T LoadPlugin<T>(string pluginAssemblyPath, string typeName)
    {
        // Ensure the pluginAssemblyPath is absolute.
        string absolutePath = Path.GetFullPath(pluginAssemblyPath);

        // Use our custom PluginLoadContext.
        var loadContext = new PluginLoadContext(absolutePath);
        Assembly pluginAssembly = loadContext.LoadFromAssemblyPath(absolutePath);

        // Get the type from the assembly.
        Type pluginType = pluginAssembly.GetType(typeName, throwOnError: true);
        object pluginInstance = Activator.CreateInstance(pluginType);

        if (!(pluginInstance is T))
        {
            throw new InvalidOperationException($"Unable to cast plugin instance to type {typeof(T).FullName}");
        }

        return (T)pluginInstance;
    }
}

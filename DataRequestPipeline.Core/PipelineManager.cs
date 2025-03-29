using DataRequestPipeline.Core.Configuration;
using DataRequestPipeline.DataContracts;
using System.ComponentModel.Composition.Hosting;
using System.Text.Json;

namespace DataRequestPipeline.Core
{
    public class PipelineManager
    {
        // Event to broadcast status messages.
        public event Action<string> StatusUpdated;

        private GlobalConfig _globalConfig;

        public PipelineManager()
        {
            // Load the global configuration at startup.
            _globalConfig = LoadGlobalConfig("global.json");
        }

        public async Task ExecutePipelineAsync()
        {
            try
            {
                // Stage 1: Setup (with rollback)
                await ExecuteStageAsync<ISetupPlugin, SetupContext>("Plugins/Setup", "setup.json");

                // Stage 2: Clean (with rollback)
                // Reusing the shared DataContext and ICleanPlugin from the previous pipeline.
                await ExecuteStageAsync<ICleanPlugin, CleanContext>("Plugins/Clean", "clean.json");

                // Stage 3: Perform Request (with connection strings)
                await ExecuteStageAsync<IPerformRequestPlugin, RequestContext>(
                    "Plugins/PerformRequest",
                    "performRequest.json",
                    context =>
                    {
                        // Inject connection strings from the global configuration.
                        context.InputConnectionString = _globalConfig.ConnectionStrings.ContainsKey("Input")
                            ? _globalConfig.ConnectionStrings["Input"]
                            : string.Empty;
                        context.OutputConnectionString = _globalConfig.ConnectionStrings.ContainsKey("Output")
                            ? _globalConfig.ConnectionStrings["Output"]
                            : string.Empty;
                    });

                /// Stage 4 : Run the test
                await ExecuteStageAsync<ITestPlugin, TestContext>("Plugins/Test", "test.json");

                // Stage 5: Export (no rollback required by design, but we still call a rollback method if needed)
                await ExecuteStageAsync<IExportPlugin, ExportContext>("Plugins/Export", "export.json");

                // Stage 6: Cleanup (rollback not needed)
                await ExecuteStageAsync<ICleanupPlugin, CleanupContext>("Plugins/Cleanup", "cleanup.json", null, hasRollback: false);

                StatusUpdated?.Invoke("DataRequestPipeline completed successfully.");
                Logger.Log("DataRequestPipeline completed successfully.");
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"Pipeline halted due to error: {ex.Message}");
                Logger.Log($"Pipeline halted due to error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generic method for executing a stage.
        /// T: Plugin interface type.
        /// C: Context type (must inherit from DataRequestBaseContext and have a parameterless constructor).
        /// </summary>
        private async Task ExecuteStageAsync<T, C>(string pluginDirectory, string configFile, Action<C> contextInitializer = null, bool hasRollback = true)
            where C : DataRequestBaseContext, new()
        {
            Logger.Log($"Starting stage for plugins in: {pluginDirectory}");

            // Read stage configuration.
            PluginConfig config = ReadPluginConfig(configFile);
            if (!config.Enabled)
            {
                Logger.Log($"Stage {pluginDirectory} is disabled via configuration.");
                return;
            }

            // Create and initialize the context.
            C context = new C();
            contextInitializer?.Invoke(context);

            // Load plugins using MEF.
            IEnumerable<T> plugins = LoadPlugins<T>(pluginDirectory);
            var executedPlugins = new List<T>();

            // Execute plugins in the order specified in the configuration.
            foreach (var pluginId in config.Plugins)
            {
                T plugin = FindPluginById(plugins, pluginId);
                if (plugin == null)
                {
                    string err = $"Plugin '{pluginId}' not found in directory {pluginDirectory}";
                    Logger.Log(err);
                    throw new Exception(err);
                }

                string startMsg = $"Starting plugin {pluginId} in stage {pluginDirectory}.";
                StatusUpdated?.Invoke(startMsg);
                Logger.Log(startMsg);

                try
                {
                    // Dynamically call ExecuteAsync on the plugin.
                    await ((dynamic)plugin).ExecuteAsync(context);
                    executedPlugins.Add(plugin);
                }
                catch (Exception ex)
                {
                    string err = $"Error in plugin {pluginId}: {ex.Message}";
                    Logger.Log(err);
                    StatusUpdated?.Invoke(err);
                    if (hasRollback)
                    {
                        await RollbackPluginsAsync(executedPlugins, context);
                    }
                    throw;
                }
            }
        }

        private async Task RollbackPluginsAsync<T>(List<T> executedPlugins, DataRequestBaseContext context)
        {
            Logger.Log("Initiating rollback...");
            for (int i = executedPlugins.Count - 1; i >= 0; i--)
            {
                T plugin = executedPlugins[i];
                try
                {
                    await ((dynamic)plugin).RollbackAsync(context);
                    Logger.Log($"Rolled back plugin {plugin.GetType().Name} successfully.");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error rolling back plugin {plugin.GetType().Name}: {ex.Message}");
                }
            }
        }

        private IEnumerable<T> LoadPlugins<T>(string directory)
        {
            var assemblies = new List<System.Reflection.Assembly>();

            if (!Directory.Exists(directory))
            {
                Logger.Log($"Plugin directory not found: {directory}");
                return Enumerable.Empty<T>();
            }

            foreach (var file in Directory.GetFiles(directory, "*.dll"))
            {
                try
                {
                    var assembly = System.Reflection.Assembly.LoadFrom(file);
                    assemblies.Add(assembly);
                    Logger.Log($"Loaded assembly {Path.GetFileName(file)}.");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error loading assembly {file}: {ex.Message}");
                }
            }

            var catalog = new AggregateCatalog();
            foreach (var assembly in assemblies)
            {
                catalog.Catalogs.Add(new AssemblyCatalog(assembly));
            }
            var container = new CompositionContainer(catalog);

            try
            {
                return container.GetExportedValues<T>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during plugin composition: {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

        private T FindPluginById<T>(IEnumerable<T> plugins, string pluginId)
        {
            foreach (var plugin in plugins)
            {
                if (plugin.GetType().Name.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
                {
                    return plugin;
                }
            }
            return default;
        }

        private PluginConfig ReadPluginConfig(string configFile)
        {
            if (!File.Exists(configFile))
            {
                Logger.Log($"Configuration file {configFile} not found. Using default configuration.");
                return new PluginConfig();
            }

            try
            {
                string json = File.ReadAllText(configFile);
                PluginConfig config = JsonSerializer.Deserialize<PluginConfig>(json);
                Logger.Log($"Loaded configuration from {configFile}.");
                return config;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading configuration file {configFile}: {ex.Message}");
                throw;
            }
        }

        private GlobalConfig LoadGlobalConfig(string configFile)
        {
            if (!File.Exists(configFile))
            {
                Logger.Log($"Global configuration file {configFile} not found. Using default global configuration.");
                return new GlobalConfig();
            }

            try
            {
                string json = File.ReadAllText(configFile);
                GlobalConfig config = JsonSerializer.Deserialize<GlobalConfig>(json);
                Logger.Log($"Loaded global configuration from {configFile}.");
                return config;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading global configuration file {configFile}: {ex.Message}");
                throw;
            }
        }
    }
}

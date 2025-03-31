using DataRequestPipeline.Core;
using System.Reflection;

namespace DataRequestPipeline.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Log("DataRequestPipeline starting...");

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Console.WriteLine($"Loaded: {asm.FullName}");
            }
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            // Instantiate the PipelineManager
            var pipelineManager = new PipelineManager();
            pipelineManager.StatusUpdated += (status) => Console.WriteLine($"STATUS: {status}");

            try
            {
                // Execute the entire pipeline asynchronously
                await pipelineManager.ExecutePipelineAsync();
                Logger.Log("DataRequestPipeline completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("Pipeline execution encountered an error: " + ex.Message);
            }

            Logger.Log("DataRequestPipeline finished. Press any key to exit...");
            Console.ReadKey();
        }
        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            // Look for the DataRequestPipeline.DataContracts assembly
            AssemblyName requestedName = new AssemblyName(args.Name);
            if (requestedName.Name.Equals("DataRequestPipeline.DataContracts", StringComparison.OrdinalIgnoreCase))
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string assemblyPath = Path.Combine(basePath, "DataRequestPipeline.DataContracts.dll");
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }
            return null;
        }
    }
}

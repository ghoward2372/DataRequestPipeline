using DataRequestPipeline.Core;

namespace DataRequestPipeline.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Log("DataRequestPipeline starting...");

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
    }
}

namespace DataRequestPipeline.DataContracts
{
    /// <summary>
    /// Shared context that holds data and state across pipeline stages.
    /// </summary>
    public class DataContext
    {
        public string Data { get; set; } = "Initial data";
        // Add other shared properties as needed.
    }
}

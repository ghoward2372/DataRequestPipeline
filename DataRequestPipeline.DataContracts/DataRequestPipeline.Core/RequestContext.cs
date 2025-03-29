namespace DataRequestPipeline.Core
{
    /// <summary>
    /// Context for the Perform Request stage.
    /// </summary>
    public class RequestContext : DataRequestBaseContext
    {
        public string InputConnectionString { get; set; }
        public string OutputConnectionString { get; set; }
        // Add additional request-specific properties here.
    }
}

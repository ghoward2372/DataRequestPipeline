namespace DataRequestPipeline.Core
{
    /// <summary>
    /// Base context for all stages that provides a shared list of added table names.
    /// </summary>
    public class DataRequestBaseContext
    {
        public List<string> AddedTableNames { get; } = new List<string>();
    }
}

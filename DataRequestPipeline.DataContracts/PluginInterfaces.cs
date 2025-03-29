using DataRequestPipeline.Core;

namespace DataRequestPipeline.DataContracts
{
    public interface ISetupPlugin
    {
        Task ExecuteAsync(SetupContext context);
        Task RollbackAsync(SetupContext context);
    }

    public interface IPerformRequestPlugin
    {
        Task ExecuteAsync(RequestContext context);
        Task RollbackAsync(RequestContext context);
    }

    public interface IExportPlugin
    {
        Task ExecuteAsync(ExportContext context);
        Task RollbackAsync(ExportContext context);
    }

    public interface ICleanupPlugin
    {
        Task ExecuteAsync(CleanupContext context);
    }

    /// <summary>
    /// Interface for data cleaning plugins.
    /// </summary>
    public interface ICleanPlugin
    {
        Task ExecuteAsync(DataContext context);
        Task RollbackAsync(DataContext context);
    }
}
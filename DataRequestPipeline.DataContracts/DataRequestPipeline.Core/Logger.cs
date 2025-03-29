namespace DataRequestPipeline.Core
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string LogFilePath = "DataRequestPipeline.log";

        public static void Log(string message)
        {
            lock (_lock)
            {
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                Console.WriteLine(logMessage);
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
        }
    }
}

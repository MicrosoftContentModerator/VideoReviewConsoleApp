using System;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    using System.IO;


    public static class Logger
    {
        private static string logFilepath = string.Empty;

        public static void Log(string message)
        {
            logFilepath = AmsConfigurations.logFilePath;
            try
            {
                using (StreamWriter w = File.AppendText(logFilepath))
                {
                    w.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

using System;
using System.IO;
using System.Net;
using FilesWatcher.SVN;
using Microsoft.Extensions.Configuration;

namespace FilesWatcher
{
    public class Program
    {
        private static void Main()
        {
            Console.Title = "Files watcher";

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var config = builder.Build();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Log.ShowLog("---------------Start monitoring folder---------------");
            Log.ShowLog($"Folder path: { config["FolderPath"] } " + Environment.NewLine +
                        $"Integration svn path: { config["IntegrationSvnPath"] } " + Environment.NewLine +
                        $"Sound designer svn path: { config["SoundDesignerSvnPath"] } ");

            var svnCredential = new NetworkCredential(config["SoundDesignerRepositoryUsername"], config["SoundDesignerRepositoryPassword"]);
            var svnClient = new SharpSvnClient(svnCredential);

            try
            {
                var watcher = new Watcher(config, svnClient);
                watcher.RunWatcher();
            }
            catch (Exception exception)
            {
                Log.ShowException(exception.Message);
            }

            Console.ReadKey();
        }
    }
}
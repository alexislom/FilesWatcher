using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace FilesWatcher
{
    public class Program
    {
        private static void Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var config = builder.Build();

            Console.WriteLine("---------------Start monitoring folder---------------");

            Console.WriteLine($"Folder path: { config["FolderPath"] } " + Environment.NewLine +
                              $"Integration svn path: { config["IntegrationSvnPath"] } " + Environment.NewLine +
                              $"Sound designer svn path: { config["SoundDesignerSvnPath"] } ");

            RunWatcher(config);

        }

        private static void RunWatcher(IConfiguration config)
        {
            using (var watcher = new FileSystemWatcher())
            {
                watcher.Path = config["FolderPath"];

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                watcher.NotifyFilter = NotifyFilters.CreationTime  |
                                       NotifyFilters.LastAccess    |
                                       NotifyFilters.LastWrite     |
                                       NotifyFilters.FileName      |
                                       NotifyFilters.DirectoryName;

                // Only watch text files.
                watcher.Filter = string.Empty; //config["Filter"]; //"*.*";//"*.txt";

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;
            }
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e) =>
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");

        private static void OnRenamed(object source, RenamedEventArgs e) =>
            // Specify what is done when a file is renamed.
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
    }
}
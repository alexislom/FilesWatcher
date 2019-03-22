using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace FilesWatcher
{
    public class Program
    {
        public static IConfigurationRoot Config { get; private set; }

        private static void Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Config = builder.Build();

            Console.WriteLine("---------------Start monitoring folder---------------");

            Console.WriteLine($"Folder path: { Config["FolderPath"] } " + Environment.NewLine +
                              $"Integration svn path: { Config["IntegrationSvnPath"] } " + Environment.NewLine +
                              $"Sound designer svn path: { Config["SoundDesignerSvnPath"] } ");

            RunWatcher();
        }

        private static void RunWatcher()
        {
            using (var watcher = new FileSystemWatcher())
            {
                watcher.Path = Config["FolderPath"];

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                watcher.NotifyFilter = NotifyFilters.CreationTime |
                                       NotifyFilters.Size |
                                       NotifyFilters.LastAccess |
                                       NotifyFilters.LastWrite |
                                       NotifyFilters.FileName |
                                       NotifyFilters.DirectoryName;

                // Watch all files.
                watcher.Filter = string.Empty;

                watcher.IncludeSubdirectories = true;

                // Add event handlers.
                watcher.Changed += OnChanged;
                //watcher.Created += OnChanged;
                //watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                Console.WriteLine("Press 'q' to quit the files watcher.");
                while (Console.Read() != 'q')
                {
                }
            }
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");

            try
            {
                ExtractToZip(e);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        private static void ExtractToZip(FileSystemEventArgs e)
        {
            string zipPath;
            string folderPath;

            var extension = Path.GetExtension(e.FullPath);
            if (!string.IsNullOrEmpty(extension))
            {
                folderPath = Path.GetDirectoryName(e.FullPath);
                var lastFolderName = Path.GetFileName(folderPath);
                zipPath = Path.Combine(Config["ZipPath"], $"{lastFolderName}.zip");
            }
            else
            {
                zipPath = Path.Combine(Config["ZipPath"], $"{e.Name}.zip");
                folderPath = e.FullPath;
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(folderPath, zipPath);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
        }
    }
}
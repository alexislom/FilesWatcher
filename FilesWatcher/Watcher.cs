using System;
using System.IO;
using System.Runtime.Caching;
using System.Threading.Tasks;
using FilesWatcher.SVN;
using Microsoft.Extensions.Configuration;
using NReco.VideoConverter;

namespace FilesWatcher
{
    //Robust solution for FileSystemWatcher firing events multiple times
    //http://benhall.io/a-robust-solution-for-filesystemwatcher-firing-events-multiple-times/
    public class Watcher
    {
        private static MemoryCache _memCache;
        private static CacheItemPolicy _cacheItemPolicy;
        private static ISvnClient _svnClient;
        private static IConfigurationRoot Config { get; set; }

        public Watcher(IConfigurationRoot config, ISvnClient svnClient)
        {
            Config = config;
            _svnClient = svnClient;

            _memCache = MemoryCache.Default;

            _cacheItemPolicy = new CacheItemPolicy
            {
                RemovedCallback = OnRemovedFromCache
            };
        }

        public void RunWatcher()
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
                watcher.Created += OnChanged;
                //watcher.Deleted += OnChanged;
                watcher.Renamed += OnChanged;//OnRenamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                Console.WriteLine("Press 'q' to quit the files watcher.");
                while (Console.Read() != 'q')
                {
                }
            }
        }

        // Add file event to cache for CacheTimeMilliseconds
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            _cacheItemPolicy.AbsoluteExpiration = DateTimeOffset.Now.Add(TimeSpan.FromMilliseconds(100));
            // Only add if it is not there already (swallow others)
            _memCache.AddOrGetExisting(e.Name, e, _cacheItemPolicy);
        }

        // Handle cache item expiring 
        private static void OnRemovedFromCache(CacheEntryRemovedArguments args)
        {
            if (args.RemovedReason != CacheEntryRemovedReason.Expired)
                return;

            // Now actually handle file event
            var e = (FileSystemEventArgs)args.CacheItem.Value;

            if (e.FullPath.Contains("_postfix"))
                return;

            if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                if (e is RenamedEventArgs eventArgs)
                    Console.WriteLine($"File: {eventArgs.OldFullPath} renamed to {eventArgs.FullPath}");
            }
            else
            {
                Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
            }

            try
            {
                var extension = Path.GetExtension(e.FullPath);
                if (string.IsNullOrEmpty(extension))
                    return;

                if (extension == ".avi")
                {
                    ConverAviFileToMp4(e);
                }
                else
                {
                    MoveFileToSharedFolder(e);
                }
            }
            catch (Exception exception)
            {
                ShowError(exception.Message);
            }
        }

        private static void ConverAviFileToMp4(FileSystemEventArgs e)
        {
            var mp4FilePath = Path.Combine(Config["SoundDesignerSvnPath"], $"{Path.GetFileNameWithoutExtension(e.Name)}.mp4");
            if (File.Exists(mp4FilePath))
            {
                File.Delete(mp4FilePath);
            }

            var outputPath = Path.Combine(Config["SoundDesignerSvnPath"], $"{Path.GetFileNameWithoutExtension(e.Name)}.mp4");

            var ffMpeg = new FFMpegConverter();
            Task.Run(() => ffMpeg.ConvertMedia(e.FullPath, outputPath, Format.mp4));

            //if (_svnClient.SvnAdd(outputPath))
            //{
            //    _svnClient.SvnCommit(outputPath);
            //}
        }

        private static void MoveFileToSharedFolder(FileSystemEventArgs e)
        {
            try
            {
                var fileName = Path.GetFileName(e.FullPath);
                var directoryName = Path.GetFileName(Path.GetDirectoryName(e.FullPath));

                var pathToSharedFolder = Path.Combine(Config["IntegrationSvnPath"], $"{directoryName}");

                if (!Directory.Exists(pathToSharedFolder))
                {
                    // Try to create the directory.
                    Directory.CreateDirectory(pathToSharedFolder);
                }

                var pathToSharedFile = Path.Combine(pathToSharedFolder, $"{fileName}");

                if (File.Exists(pathToSharedFile))
                    return;

                File.Copy(e.FullPath, pathToSharedFile);
            }
            catch (Exception exception)
            {
                ShowError(exception.Message);
            }
        }

        public static void ShowError(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            //Console.WriteLine(message);
            Console.Error.WriteLine(message);
            // restore color
            Console.ForegroundColor = originalColor;
        }
    }
}
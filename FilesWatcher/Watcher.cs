using System;
using System.IO;
using System.Runtime.Caching;
using System.Threading.Tasks;
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
        //private const int CacheTimeMilliseconds = 1000;
        private static IConfigurationRoot Config { get; set; }

        public Watcher(IConfigurationRoot config)
        {
            Config = config;

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

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            _cacheItemPolicy.AbsoluteExpiration = DateTimeOffset.Now.Add(TimeSpan.FromMilliseconds(100));
            _memCache.AddOrGetExisting(e.Name, e, _cacheItemPolicy);
        }

        // Handle cache item expiring 
        private static void OnRemovedFromCache(CacheEntryRemovedArguments args)
        {
            if (args.RemovedReason != CacheEntryRemovedReason.Expired)
                return;

            var e = (FileSystemEventArgs)args.CacheItem.Value;

            //Console.WriteLine($"Let's now respond to the event {e.ChangeType} on {e.FullPath}");
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
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
                Console.Error.WriteLine(exception.Message);
            }
        }

        private static void ConverAviFileToMp4(FileSystemEventArgs e)
        {
            var ffMpeg = new FFMpegConverter();
            var mp4FilePath = Path.Combine(Config["SoundDesignerSvnPath"], $"{Path.GetFileNameWithoutExtension(e.Name)}.mp4");
            if (File.Exists(mp4FilePath))
            {
                File.Delete(mp4FilePath);
            }

            Task.Run(() => ffMpeg.ConvertMedia(e.FullPath, Path.Combine(Config["SoundDesignerSvnPath"], $"{Path.GetFileNameWithoutExtension(e.Name)}.mp4"), Format.mp4));
        }

        private static void MoveFileToSharedFolder(FileSystemEventArgs e)
        {
            var fileName = Path.GetFileName(e.FullPath);
            var pathToSharedFolder = Path.Combine(Config["IntegrationSvnPath"], $"{fileName}");

            if (File.Exists(pathToSharedFolder))
                return;

            File.Copy(e.FullPath, pathToSharedFolder);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
        }
    }
}
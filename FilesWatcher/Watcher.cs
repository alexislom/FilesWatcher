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

            if (e.FullPath.ToLowerInvariant().Contains("_Collect".ToLowerInvariant()))
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
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        WorkOnCreateOrRenameEvent(e);
                        break;
                    case WatcherChangeTypes.Changed:
                        WorkOnChangeEvent(e);
                        break;
                    case WatcherChangeTypes.Renamed:
                        WorkOnCreateOrRenameEvent(e);
                        break;
                    //case WatcherChangeTypes.Deleted:
                    //    WorkOnDeleteEvent(e);
                    //    break;
                }
            }
            catch (Exception exception)
            {
                Log.ShowException(exception.Message);
            }
        }

        #region Event handlers

        private static void WorkOnCreateOrRenameEvent(FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                var extension = Path.GetExtension(e.FullPath);
                if (!string.IsNullOrEmpty(extension))
                {
                    if (extension == ".avi")
                    {
                        ConverAviFileToMp4(e);
                    }
                    else
                    {
                        CopyOfFile(e);
                    }
                }
            }
            else if (Directory.Exists(e.FullPath))
            {
                RecursiveCopyOfDirectory(e);
            }
        }

        private static void WorkOnChangeEvent(FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                var extension = Path.GetExtension(e.FullPath);
                if (!string.IsNullOrEmpty(extension) && extension != ".avi")
                {
                    CopyOfFile(e);
                }
            }
        }

        //private static void WorkOnRenameEvent(FileSystemEventArgs fileSystemEventArgs)
        //{
        //}

        //private static void WorkOnDeleteEvent(FileSystemEventArgs fileSystemEventArgs)
        //{
        //}

        #endregion Event handlers

        #region Private methods

        private static void CopyOfFile(FileSystemEventArgs e)
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

        private static void RecursiveCopyOfDirectory(FileSystemEventArgs e)
        {
            var directoryName = Path.GetFileName(e.FullPath);

            var targetPath = Path.Combine(Config["IntegrationSvnPath"], $"{directoryName}");

            if (!Directory.Exists(targetPath))
            {
                // Try to create the directory.
                Directory.CreateDirectory(targetPath);
            }

            var diSource = new DirectoryInfo(e.FullPath);
            var diTarget = new DirectoryInfo(targetPath);

            RecursiveCopy(diSource, diTarget);
        }

        public static void RecursiveCopy(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (var fileInfo in source.GetFiles())
            {
                fileInfo.CopyTo(Path.Combine(target.FullName, fileInfo.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                RecursiveCopy(diSourceSubDir, nextTargetSubDir);
            }
        }

        private static void ConverAviFileToMp4(FileSystemEventArgs e)
        {
            var outputPath = Path.Combine(Config["SoundDesignerSvnPath"], $"{Path.GetFileNameWithoutExtension(e.Name)}.mp4");
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var ffMpeg = new FFMpegConverter();
            Task.Run(() =>
            {
                ffMpeg.ConvertMedia(e.FullPath, outputPath, Format.mp4);

                if (_svnClient.SvnAdd(outputPath))
                {
                    _svnClient.SvnCommit(outputPath);
                }
            });
        }

        #endregion Private methods

        private static void RecursiveCopyFilesToSharedFolder(FileSystemEventArgs e)
        {
            //var fileName = Path.GetFileName(e.FullPath);
            var directoryName = Path.GetFileName(Path.GetDirectoryName(e.FullPath));

            //var pathToSharedFolder = Path.Combine(Config["IntegrationSvnPath"], $"{directoryName}");
            var targetPath = Path.Combine(Config["IntegrationSvnPath"], $"{directoryName}");

            //if (!Directory.Exists(pathToSharedFolder))
            //{
            //    // Try to create the directory.
            //    Directory.CreateDirectory(pathToSharedFolder);
            //}
            if (!Directory.Exists(targetPath))
            {
                // Try to create the directory.
                Directory.CreateDirectory(targetPath);
            }

            //var pathToSharedFile = Path.Combine(pathToSharedFolder, $"{fileName}");

            //if (File.Exists(pathToSharedFile))
            //    return;

            //File.Copy(e.FullPath, pathToSharedFile);

            if (!Directory.Exists(e.FullPath))
                return;

            var files = Directory.GetFiles(e.FullPath);

            // Copy the files and overwrite destination files if they already exist.
            foreach (var s in files)
            {
                // Use static Path methods to extract only the file name from the path.
                var fileName = Path.GetFileName(s);
                var destFile = Path.Combine(targetPath, fileName);
                File.Copy(s, destFile, true);
            }
        }
    }
}
﻿using System;
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
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var config = builder.Build();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("---------------Start monitoring folder---------------");
            Console.WriteLine($"Folder path: { config["FolderPath"] } " + Environment.NewLine +
                              $"Integration svn path: { config["IntegrationSvnPath"] } " + Environment.NewLine +
                              $"Sound designer svn path: { config["SoundDesignerSvnPath"] } ");


            var svnCredential = new NetworkCredential(config["SoundDesignerRepositoryUsername"], config["SoundDesignerRepositoryPassword"]);
            var svnClient = new SharpSvnClient(svnCredential);

            var watcher = new Watcher(config, svnClient);
            watcher.RunWatcher();
        }
    }
}
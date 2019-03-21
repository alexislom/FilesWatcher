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


            Console.WriteLine($"Folder path: { config["FolderPath"] } " + Environment.NewLine +
                              $"Integration svn path: { config["IntegrationSvnPath"] } " + Environment.NewLine +
                              $"Sound designer svn path: { config["SoundDesignerSvnPath"] } ");
        }
    }
}
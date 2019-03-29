using System;
using SharpSvn;

namespace FilesWatcher
{
    public static class Log
    {
        private static ConsoleColor _originalColor;

        public static void ShowLog(string message) => Console.WriteLine(message);

        public static void ShowException(string message)
        {
            UseRedColor();
            Console.Error.WriteLine(message);
            RestoreColor();
        }

        public static void ShowSvnException(string path, SvnException svnException)
        {
            UseRedColor();
            Console.WriteLine($"Error: {svnException.SvnErrorCode}, SharpSvn error on: {path}, {svnException.RootCause.Message}. {svnException}.");
            RestoreColor();
        }

        private static void RestoreColor() => Console.ForegroundColor = _originalColor;

        private static void UseRedColor()
        {
            _originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
        }
    }
}
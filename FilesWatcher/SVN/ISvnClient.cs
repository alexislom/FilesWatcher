using System;

namespace FilesWatcher.SVN
{
    public interface ISvnClient
    {
        bool IsWorkingCopy(string path);
        bool SvnCheckOut(Uri uri, string path);
        bool SvnAdd(string path);
        bool SvnCommit(string path);
        bool SvnDelete(string path);
    }
}
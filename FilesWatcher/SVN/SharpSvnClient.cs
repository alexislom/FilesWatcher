using System;
using System.Net;
using SharpSvn;

namespace FilesWatcher.SVN
{
    public class SharpSvnClient : ISvnClient
    {
        private readonly ICredentials _svnCredentials;

        public SharpSvnClient(ICredentials svnCredentials)
        {
            _svnCredentials = svnCredentials;
        }

        public bool IsWorkingCopy(string path)
        {
            using (var svnClient = new SvnClient())
            {
                svnClient.Authentication.DefaultCredentials = _svnCredentials;
                var uri = svnClient.GetUriFromWorkingCopy(path);
                return uri != null;
            }
        }

        public bool SvnCheckOut(Uri uri, string path)
        {
            return DoWithSvn(path, svnClient => {
                svnClient.Authentication.DefaultCredentials = _svnCredentials;
                var url = new SvnUriTarget(uri);
                var args = new SvnCheckOutArgs { Depth = SvnDepth.Infinity, IgnoreExternals = true };
                var result = svnClient.CheckOut(url, path, args);
                if (result)
                    Log.ShowLog($"SharpSvn has checked out {path}");

                return result;
            });
        }

        public bool SvnAdd(string path)
        {
            return DoWithSvn(path, svnClient => {
                svnClient.Authentication.DefaultCredentials = _svnCredentials;
                var args = new SvnAddArgs { Depth = SvnDepth.Infinity, AddParents = true };
                var result = svnClient.Add(path, args);
                if (result)
                    Log.ShowLog($"SharpSvn has added {path}");

                return result;
            });
        }

        public bool SvnCommit(string path)
        {
            return DoWithSvn(path, svnClient => {
                svnClient.Authentication.DefaultCredentials = _svnCredentials;
                var args = new SvnCommitArgs
                {
                    LogMessage = $"Commited by FilesWatcher service via SharpSvn. {path}",
                    Depth = SvnDepth.Infinity
                };

                var result = svnClient.Commit(path, args, out var svnCommitResult);
                if (result)
                {
                    Log.ShowLog(svnCommitResult != null ? $"SharpSvn has committed {path}"
                                                        : $"SharpSvn tried to commit {path}, but no modification was detected");
                }

                return result;
            });
        }

        public bool SvnDelete(string path)
        {
            return DoWithSvn(path, svnClient => {
                svnClient.Authentication.DefaultCredentials = _svnCredentials;
                var args = new SvnDeleteArgs
                {
                    Force = true,
                    KeepLocal = false,
                    LogMessage = $"Deleted by FilesWatcher service via SharpSvn. {path}"
                };
                var result = svnClient.Delete(path, args);
                if (result)
                    Log.ShowLog($"SharpSvn has deleted {path}");

                return result;
            });
        }

        private bool DoWithSvn(string path, Func<SvnClient, bool> action)
        {
            using (var svnClient = new SvnClient())
            {
                try
                {
                    return action(svnClient);
                }
                catch (SvnException se)
                {
                    Log.ShowSvnException(path, se);
                }
            }
            return false;
        }
    }
}
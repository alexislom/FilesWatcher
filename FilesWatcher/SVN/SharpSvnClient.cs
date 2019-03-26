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
                    Console.WriteLine($"SharpSvn has checked out {path}");

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
                    Console.WriteLine($"SharpSvn has added {path}");

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

                SvnCommitResult svnCommitResult = null;
                var result = svnClient.Commit(path, args, out svnCommitResult);
                if (result)
                {
                    Console.WriteLine(svnCommitResult != null ? $"SharpSvn has committed {path}"
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
                    Console.WriteLine($"SharpSvn has deleted {path}");

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
                    Console.WriteLine($"Error: {se.SvnErrorCode}, SharpSvn error on: {path}, {se.RootCause.Message}. {se}.");
                }
            }
            return false;
        }
    }
}
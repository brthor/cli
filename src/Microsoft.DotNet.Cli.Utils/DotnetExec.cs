using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.DotNet.Cli.Utils
{
    public static class DotnetExec
    {
        internal static string _hostDir;
        internal static string _hostExePath;

        /// <summary>
        /// Gets the path to the version of corehost that was shipped with this command
        /// </summary>
        public static string LocalHostExePath => Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, Constants.DotnetHostExecutableName);

        public static string HostExePath
        {
            get
            {
                if (_hostExePath == null)
                {
                    _hostExePath = Path.Combine(HostDir, Constants.DotnetHostExecutableName);
                }
                return _hostExePath;
            }
        }

        private static string HostDir
        {
            get
            {
                if (_hostDir == null)
                {
                    _hostDir = Path.GetDirectoryName(Env.GetCommandPath(
                        Constants.DotnetHostExecutableName, new[] { string.Empty }));
                }

                return _hostDir;
            }
        }

        public static void CopyTo(string destinationPath, string hostExeName)
        {
            foreach (var binaryName in Constants.HostBinaryNames)
            {
                var outputBinaryName = binaryName.Equals(Constants.DotnetHostExecutableName)
                                     ? hostExeName : binaryName;
                var outputBinaryPath = Path.Combine(destinationPath, outputBinaryName);
                var hostBinaryPath = Path.Combine(HostDir, binaryName);
                File.Copy(hostBinaryPath, outputBinaryPath, overwrite: true);
            }
        }
    }
}

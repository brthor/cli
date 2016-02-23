using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public class YumDependencyUtility
    {
        private SharedLibraryDependencyUtility _sharedLibraryDependencyUtility;

        internal YumDependencyUtility()
        {
            _sharedLibraryDependencyUtility = new SharedLibraryDependencyUtility();
        }

        internal bool YumPackageIsInstalled(string packageName)
        {
            var result = Command.Create("yum", "list", "installed", packageName)
                .CaptureStdOut()
                .CaptureStdErr()
                .QuietBuildReporter()
                .Execute();

            return result.ExitCode == 0;
        }
    }
}

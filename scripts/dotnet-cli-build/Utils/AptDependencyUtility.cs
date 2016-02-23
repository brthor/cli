using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public class AptDependencyUtility
    {
        private SharedLibraryDependencyUtility _sharedLibraryDependencyUtility;

        internal AptDependencyUtility()
        {
            _sharedLibraryDependencyUtility = new SharedLibraryDependencyUtility();
        }

        internal IEnumerable<string> DeterminePackageDependenciesOfDirectory(string binPath)
        {
            var sharedLibraries = GetSharedLibraryFiles(binPath);
            var sharedLibrariesDependencies = _sharedLibraryDependencyUtility.DetermineSharedLibrariesDependencies(sharedLibraries);
            var packageDependencies = DeterminePackageDependencies(sharedLibrariesDependencies);

            return packageDependencies;
        }

        internal bool AptPackageIsInstalled(string packageName)
        {
            var result = Command.Create("dpkg", "-s", packageName)
                .CaptureStdOut()
                .CaptureStdErr()
                .QuietBuildReporter()
                .Execute();

            return result.ExitCode == 0;
        }

        private IEnumerable<string> GetSharedLibraryFiles(string path)
        {
            return Directory.EnumerateFiles(path, "*.so", SearchOption.TopDirectoryOnly);
        }
        
        private IEnumerable<string> DeterminePackageDependencies(IEnumerable<string> sharedLibraries)
        {
            var packageDependencies = new List<string>();

            foreach (var sharedLibrary in sharedLibraries)
            {
                var aptFileSearchOutput = RunAptFileSearch(sharedLibrary);

                var packageDependencyCandidates = ParsePackageNamesFromAptFileSearchOutput(aptFileSearchOutput);
                HandleAmbiguousPackageDependencies(sharedLibrary, packageDependencyCandidates);

                var packageDependency = packageDependencies.FirstOrDefault();
                packageDependencies.Add(packageDependency);
            }

            return packageDependencies;
        }

        private void HandleAmbiguousPackageDependencies(string sharedLibrary, IEnumerable<string> packageDependencyCandidates)
        {
            if (packageDependencyCandidates.Count() > 1)
            {
                throw new AmbiguousPackageDependencyException(
                    $"{sharedLibrary} has ambiguous package dependency candidates: {string.Join(",", packageDependencyCandidates)}");
            }
        }

        private string RunAptFileSearch(string sharedLibrary)
        {
            var result = Command.Create("apt-file", "search", "-l", "--regex", ".*?" + sharedLibrary)
                .CaptureStdErr()
                .CaptureStdOut()
                .QuietBuildReporter()
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new Exception("apt-file search invocation failed");
            }

            return result.StdOut;
        }

        private IEnumerable<string> ParsePackageNamesFromAptFileSearchOutput(string aptFileSearchOutput)
        {
            return aptFileSearchOutput
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());
        }

        internal class AmbiguousPackageDependencyException : Exception
        {
            public AmbiguousPackageDependencyException(string message) : base(message) { }
        }

    }
}

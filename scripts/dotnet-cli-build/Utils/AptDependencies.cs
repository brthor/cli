using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public class AptDependencies
    {
        public IEnumerable<string> DetermineCoreClrAndCoreFXPackageDependencies(string binPath)
        {
            var sharedLibraries = GetSharedLibraryFiles(binPath);
            var sharedLibrariesDependencies = DetermineSharedLibrariesDependencies(sharedLibraries);
            var packageDependencies = DeterminePackageDependencies(sharedLibrariesDependencies);

            return packageDependencies;
        }

        private IEnumerable<string> GetSharedLibraryFiles(string path)
        {
            return Directory.EnumerateFiles(path, "*.so", SearchOption.TopDirectoryOnly);
        }

        private IEnumerable<string> DetermineSharedLibrariesDependencies(IEnumerable<string> sharedLibraries)
        {
            var sharedLibraryDependencies = new List<string>();

            foreach (var sharedLibrary in sharedLibraryDependencies)
            {
                sharedLibraryDependencies.AddRange(DetermineSharedLibraryDependencies(sharedLibrary));
            }

            return sharedLibraryDependencies;
        }

        private IEnumerable<string> DetermineSharedLibraryDependencies(string sharedLibrary)
        {
            var lddOutput = RunLdd(sharedLibrary);

            var sharedLibraryDependencies = ParseDependenciesFromLddOutput(lddOutput);

            return sharedLibraryDependencies;
        }

        private string RunLdd(string sharedLibrary)
        {
            var result = Command.Create("ldd", sharedLibrary)
                .CaptureStdErr()
                .CaptureStdOut()
                .QuietBuildReporter()
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new Exception("Ldd invocation failed.");
            }

            return result.StdOut;
        }

        private IEnumerable<string> ParseDependenciesFromLddOutput(string lddOutput)
        {
            var regex = @"(\s*|/)(?<shlib>[^/]+\.so(\.\d+)?)";

            var parsedDependencies = new List<string>();

            foreach (Match match in Regex.Matches(lddOutput, regex))
            {
                parsedDependencies.Add(match.Groups["shlib"].Value);
            }

            return parsedDependencies;
        }

        private string RunDpkgSearchS(string sharedLibrary)
        {
            var result = Command.Create("dpkg-query", "-S", sharedLibrary)
                .CaptureStdErr()
                .CaptureStdOut()
                .QuietBuildReporter()
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new Exception("dpkg-query -s invocation failed");
            }

            return result.StdOut;
        }


        public bool AptPackageIsInstalled(string packageName)
        {
            var result = Command.Create("dpkg", "-s", packageName)
                .CaptureStdOut()
                .CaptureStdErr()
                .QuietBuildReporter()
                .Execute();

            return result.ExitCode == 0;
        }
    }
}

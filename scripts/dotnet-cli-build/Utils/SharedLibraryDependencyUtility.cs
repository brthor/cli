using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public class SharedLibraryDependencyUtility
    {
        internal IEnumerable<string> DetermineSharedLibrariesDependencies(IEnumerable<string> sharedLibraries)
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
    }
}

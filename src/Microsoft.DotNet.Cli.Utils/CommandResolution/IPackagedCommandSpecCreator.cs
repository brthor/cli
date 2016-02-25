using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel.Graph;

namespace Microsoft.DotNet.Cli.Utils
{
    public interface IPackagedCommandSpecCreator
    {
        CommandSpec CreateCommandSpecFromLibrary(
            LockFilePackageLibrary library,
            string commandName,
            IEnumerable<string> commandArguments,
            IEnumerable<string> allowedExtensions,
            string nugetPackagesRoot,
            string depsFilePath = null);
        
    }
}

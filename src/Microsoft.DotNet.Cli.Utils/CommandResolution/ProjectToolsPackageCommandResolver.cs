using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.Extensions.PlatformAbstractions;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace Microsoft.DotNet.Cli.Utils
{
    internal class ProjectToolsPackageCommandResolver : ICommandResolver
    {
        private static readonly NuGetFramework s_toolPackageFramework = FrameworkConstants.CommonFrameworks.DnxCore50;

        private List<string> _allowedCommandExtensions;
        private IPackagedCommandSpecCreator _packagedCommandSpecCreator;

        public ProjectToolsPackageCommandResolver(IPackagedCommandSpecCreator packagedCommandSpecCreator)
        {
            _packagedCommandSpecCreator = packagedCommandSpecCreator;

            _allowedCommandExtensions = new List<string>() 
            {
                FileNameSuffixes.DotNet.DynamicLib
            };
        }

        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            return ResolveFromProjectTools(
                commandResolverArguments.CommandName, 
                commandResolverArguments.CommandArguments,
                commandResolverArguments.ProjectDirectory);
        }

        public CommandSpec ResolveFromProjectTools(
            string commandName, 
            IEnumerable<string> args,
            string projectDirectory)
        {
            var projectContext = GetProjectContextFromDirectory(projectDirectory, s_toolPackageFramework);

            if (projectContext == null)
            {
                return null;
            }

            var toolsLibraryNames = projectContext.ProjectFile.Tools.Select(t => t.Name);

            if (toolsLibraryNames == null || toolsLibraryNames.Count() == 0)
            {
                return null;
            }

            foreach (var toolLibraryName in toolsLibraryNames)
            {
                var commandSpec = ResolveCommandSpecFromTool(commandName, args, toolLibraryName, projectContext);

                if (commandSpec != null)
                {
                    return commandSpec;
                }
            }

            return null;
        }

        private CommandSpec ResolveCommandSpecFromTool(
            string commandName,
            IEnumerable<string> args,
            string toolLibraryName,
            ProjectContext projectContext)
        {
            //todo: change this for new resolution strategy
            var lockPath = Path.Combine(
                projectContext.ProjectDirectory, 
                "artifacts", "Tools", commandName,
                "project.lock.json"); 

            if (!File.Exists(lockPath))
            {
                return null;
            }

            var lockFile = LockFileReader.Read(lockPath);

            var lockFilePackageLibrary = lockFile.PackageLibraries.FirstOrDefault(l => l.Name == toolLibraryName);
            
            var nugetPackagesRoot = projectContext.PackagesDirectory;

            return _packagedCommandSpecCreator.CreateCommandSpecFromLibrary(
                    lockFilePackageLibrary,
                    commandName,
                    args,
                    _allowedCommandExtensions,
                    projectContext.PackagesDirectory,
                    null);
        }

        private ProjectContext GetProjectContextFromDirectory(string directory, NuGetFramework framework)
        {
            if (directory == null || framework == null)
            {
                return null;
            }

            var projectRootPath = directory;

            if (!File.Exists(Path.Combine(projectRootPath, Project.FileName)))
            {
                return null;
            }

            var projectContext = ProjectContext.Create(
                projectRootPath, 
                framework, 
                PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers());

            if (projectContext.RuntimeIdentifier == null)
            {
                return null;
            }

            return projectContext;
        }
    }
}

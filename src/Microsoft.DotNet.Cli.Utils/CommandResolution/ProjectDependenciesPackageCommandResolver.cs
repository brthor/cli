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
    internal class ProjectDependenciesPackageCommandResolver : ICommandResolver
    {
        private List<string> _allowedCommandExtensions;
        private IPackagedCommandSpecCreator _packagedCommandSpecCreator;

        public ProjectDependenciesPackageCommandResolver(IPackagedCommandSpecCreator packagedCommandSpecCreator)
        {
            _packagedCommandSpecCreator = packagedCommandSpecCreator;
        }

        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            SetAllowedCommandExtensionsFromEnvironment(commandResolverArguments.Environment);

            if (commandResolverArguments.Framework == null)
            {
                return null;
            }

            var projectContext = GetProjectContextFromDirectory(
                commandResolverArguments.ProjectDirectory, 
                commandResolverArguments.Framework);

            if (projectContext == null)
            { 
                return null;
            }

            var depsFilePath = projectContext.GetOutputPaths(commandResolverArguments.Configuration).RuntimeFiles.Deps;

            var lockFilePackageLibraries = projectContext.LibraryManager.GetLibraries()
                .Where(l => l.GetType() == typeof(PackageDescription))
                .Select(l => l as PackageDescription)
                .Select(p => p.Library);
                
            foreach (var lockFilePackageLibrary in lockFilePackageLibraries)
            {
                var commandSpec = _packagedCommandSpecCreator.CreateCommandSpecFromLibrary(
                        lockFilePackageLibrary,
                        commandResolverArguments.CommandName,
                        commandResolverArguments.CommandArguments,
                        _allowedCommandExtensions,
                        projectContext.PackagesDirectory,
                        depsFilePath);

                if (commandSpec != null)
                {
                    return commandSpec;
                }
            }

            return null;
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

        private void SetAllowedCommandExtensionsFromEnvironment(IEnvironmentProvider environment)
        {
            _allowedCommandExtensions = new List<string>();
            _allowedCommandExtensions.AddRange(environment.ExecutableExtensions);
            _allowedCommandExtensions.Add(FileNameSuffixes.DotNet.DynamicLib);
        }
    }
}

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
using NuGet.Versioning;

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

            var toolsLibraries = projectContext.ProjectFile.Tools;

            if (toolsLibraries == null || toolsLibraries.Count() == 0)
            {
                return null;
            }

            foreach (var toolLibrary in toolsLibraries)
            {
                var commandSpec = ResolveCommandSpecFromTool(commandName, args, toolLibrary, projectContext);

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
            LibraryRange toolLibrary,
            ProjectContext projectContext)
        {
            var lockPath = FindToolLockFile(toolLibrary, projectContext.PackagesDirectory);

            if (lockPath == null)
            {
                return null;
            }

            var lockFile = LockFileReader.Read(lockPath);

            var lockFilePackageLibrary = lockFile.PackageLibraries.FirstOrDefault(l => l.Name == toolLibrary.Name);
            
            var nugetPackagesRoot = projectContext.PackagesDirectory;

            return _packagedCommandSpecCreator.CreateCommandSpecFromLibrary(
                    lockFilePackageLibrary,
                    commandName,
                    args,
                    _allowedCommandExtensions,
                    projectContext.PackagesDirectory,
                    null);
        }

        private string FindToolLockFile(LibraryRange toolLibrary, string nugetPackagesDir)
        {
            var toolHive = Path.Combine(
                nugetPackagesDir,
                ".tools");

            var toolBaseDirectory = Path.Combine(toolHive, toolLibrary.Name);
            if (!Directory.Exists(toolBaseDirectory))
            {
                return null;
            }

            var toolVersionDirectory = ChooseBestVersionDirectory(toolBaseDirectory, toolLibrary);
            if (toolVersionDirectory == null)
            {
                return null;
            }

            var toolTfMDirectory = ChoosePreferredTfMDirectory(toolVersionDirectory, toolLibrary);
            if (toolTfMDirectory == null)
            {
                return null;
            }

            var lockPath = Path.Combine(toolTfMDirectory, "project.lock.json");
            if (!File.Exists(lockPath))
            {
                return null;
            }

            return lockPath;
        }

        private string ChooseBestVersionDirectory(string toolBaseDirectory, LibraryRange toolLibrary)
        {
            var versionDirectories = Directory.EnumerateDirectories(toolBaseDirectory);
            if (versionDirectories.Count() == 0)
            {
                return null;
            }

            var availableVersions = versionDirectories
                .Select(d => Path.GetFileName(d))
                .Select(v => new NuGetVersion(v));

            var bestVersion = toolLibrary.VersionRange.FindBestMatch(availableVersions);
            if (bestVersion == null)
            {
                return null;
            }

            return Path.Combine(toolBaseDirectory, bestVersion.ToNormalizedString());
        }

        private string ChoosePreferredTfMDirectory(string toolVersionDirectory, LibraryRange toolLibrary)
        {
            var tfmDirectories = Directory.EnumerateDirectories(toolVersionDirectory);
            if (tfmDirectories.Count() == 0)
            {
                return null;
            }

            // TODO: NETStandardApp or compatible here
            var tfmNames = tfmDirectories.Select(d => Path.GetFileName(d));

            foreach (var tfmDir in tfmDirectories)
            {
                var tfm = Path.GetFileName(tfmDir);

                if (tfm == "dnxcore50")
                {
                    return tfmDir;
                }
            }

            return tfmDirectories.First();
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

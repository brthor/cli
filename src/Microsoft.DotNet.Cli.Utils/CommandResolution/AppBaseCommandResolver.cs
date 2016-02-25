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
    internal class AppBaseCommandResolver : ICommandResolver
    {
        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            if (commandResolverArguments.CommandName == null)
            {
                return null;
            }

            var commandPath = commandResolverArguments.Environment.GetCommandPathFromRootPath(
                PlatformServices.Default.Application.ApplicationBasePath, 
                commandResolverArguments.CommandName);

            return commandPath == null
                ? null
                : CommandResolverCommon.CreateCommandSpecPreferringExe(
                    commandResolverArguments.CommandName,
                    commandResolverArguments.CommandArguments,
                    commandPath,
                    CommandResolutionStrategy.BaseDirectory,
                    commandResolverArguments.Environment);
        }
    }
}

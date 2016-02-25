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
    public class DefaultCommandResolver : ICommandResolver
    {
        private IList<ICommandResolver> _orderedCommandResolvers;

        public DefaultCommandResolver() : this(new PackagedCommandSpecCreator()) { }

        public DefaultCommandResolver(IPackagedCommandSpecCreator packagedCommandSpecCreator)
        {
            _orderedCommandResolvers = new List<ICommandResolver>
            {
                new RootedCommandResolver(),
                new ProjectDependenciesPackageCommandResolver(packagedCommandSpecCreator),
                new ProjectToolsPackageCommandResolver(packagedCommandSpecCreator),
                new AppBaseCommandResolver(),
                new PathCommandResolver()
            };
        }
        
        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            foreach (var commandResolver in _orderedCommandResolvers)
            {
                var commandSpec = commandResolver.Resolve(commandResolverArguments);

                if (commandSpec != null)
                {
                    return commandSpec;
                }
            }

            return null;
        }
    }
}

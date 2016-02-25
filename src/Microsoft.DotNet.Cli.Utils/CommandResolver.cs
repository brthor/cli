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
    internal static class CommandResolver
    {
        private static DefaultCommandResolver _defaultCommandResolver;
        private static ScriptCommandResolver _scriptCommandResolver;

        public static CommandSpec TryResolveCommandSpec(string commandName, IEnumerable<string> args, NuGetFramework framework = null, string configuration=Constants.DefaultConfiguration)
        {
            var commandResolverArgs = new CommandResolverArguments
            {
                CommandName = commandName,
                CommandArguments = args,
                Framework = framework,
                ProjectDirectory = Directory.GetCurrentDirectory(),
                Configuration = configuration,
                Environment = new EnvironmentProvider()
            };

            if (_defaultCommandResolver == null)
            {
                _defaultCommandResolver = new DefaultCommandResolver();
            }

            return _defaultCommandResolver.Resolve(commandResolverArgs);
        }
        
        public static CommandSpec TryResolveScriptCommandSpec(string commandName, IEnumerable<string> args, Project project, string[] inferredExtensionList)
        {
            var commandResolverArgs = new CommandResolverArguments
            {
                CommandName = commandName,
                CommandArguments = args,
                ProjectDirectory = project.ProjectDirectory,
                InferredExtensions = inferredExtensionList,
                Environment = new EnvironmentProvider()
            };

            if (_scriptCommandResolver == null)
            {
                _scriptCommandResolver = new ScriptCommandResolver();
            }

            return _scriptCommandResolver.Resolve(commandResolverArgs);
        }
    }
}


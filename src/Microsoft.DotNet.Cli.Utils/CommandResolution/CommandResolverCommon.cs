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
    internal class CommandResolverCommon
    {
        public static CommandSpec CreateCommandSpecPreferringExe(
           string commandName,
           IEnumerable<string> args,
           string commandPath,
           CommandResolutionStrategy resolutionStrategy,
           IEnvironmentProvider environment)
        {
            var useComSpec = false;

            if (PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Windows &&
                Path.GetExtension(commandPath).Equals(".cmd", StringComparison.OrdinalIgnoreCase))
            {
                var preferredCommandPath = environment.GetCommandPath(commandName, ".exe");

                // Use cmd if we can't find an exe
                if (preferredCommandPath == null)
                {
                    useComSpec = true;
                }
                else
                {
                    commandPath = preferredCommandPath;
                }
            }

            if (useComSpec)
            {
                return CreateCmdCommandSpec(commandPath, args, resolutionStrategy);
            }
            else
            {
                var escapedArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args);
                return new CommandSpec(commandPath, escapedArgs, resolutionStrategy);
            }
        }

        private static CommandSpec CreateCmdCommandSpec(
            string command,
            IEnumerable<string> args,
            CommandResolutionStrategy resolutionStrategy)
        {
            var comSpec = Environment.GetEnvironmentVariable("ComSpec");

            // Handle the case where ComSpec is already the command
            if (command.Equals(comSpec, StringComparison.OrdinalIgnoreCase))
            {
                command = args.FirstOrDefault();
                args = args.Skip(1);
            }
            var cmdEscapedArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForCmdProcessStart(args);

            if (ArgumentEscaper.ShouldSurroundWithQuotes(command))
            {
                command = $"\"{command}\"";
            }

            var escapedArgString = $"/s /c \"{command} {cmdEscapedArgs}\"";

            return new CommandSpec(comSpec, escapedArgString, resolutionStrategy);
        }
    }
}

﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using System;
using System.IO;

namespace Microsoft.DotNet.Tools.Publish
{
    public class Program
    {
        public static int Main(string[] args)
        {
            foreach(var arg in args)
            {
                Console.WriteLine(arg);
            }
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication();
            app.Name = "dotnet publish";
            app.FullName = ".NET Publisher";
            app.Description = "Publisher for the .NET Platform";
            app.HelpOption("-h|--help");

            var framework = app.Option("-f|--framework <FRAMEWORK>", "Target framework to compile for", CommandOptionType.SingleValue);
            var runtime = app.Option("-r|--runtime <RUNTIME_IDENTIFIER>", "Target runtime to publish for", CommandOptionType.SingleValue);
            var output = app.Option("-o|--output <OUTPUT_PATH>", "Path in which to publish the app", CommandOptionType.SingleValue);
            var configuration = app.Option("-c|--configuration <CONFIGURATION>", "Configuration under which to build", CommandOptionType.SingleValue);
            var projectPath = app.Argument("<PROJECT>", "The project to publish, defaults to the current directory. Can be a path to a project.json or a project directory");

            app.OnExecute(() =>
            {
                var publish = new PublishCommand();

                publish.Framework = framework.Value();
                // TODO: Remove default once xplat publish is enabled.
                publish.Runtime = runtime.Value() ?? RuntimeIdentifier.Current;
                publish.OutputPath = output.Value();
                publish.Configuration = configuration.Value() ?? Constants.DefaultConfiguration;

                publish.ProjectPath = projectPath.Value;
                if (string.IsNullOrEmpty(publish.ProjectPath))
                {
                    publish.ProjectPath = Directory.GetCurrentDirectory();
                }

                if (!publish.TryPrepareForPublish())
                {
                    return 1;
                }

                publish.PublishAllProjects();
                Reporter.Output.WriteLine($"Published {publish.NumberOfPublishedProjects}/{publish.NumberOfProjects} projects successfully");
                return (publish.NumberOfPublishedProjects == publish.NumberOfProjects) ? 0 : 1;
            });

            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
                Reporter.Error.WriteLine(ex.Message.Red());
                Reporter.Verbose.WriteLine(ex.ToString().Yellow());
                return 1;
            }
        }
    }
}

﻿using System;
using System.IO;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);
            
            var app = SetupApp();
            
            return ExecuteApp(app, args);
        }
        
        private static int ExecuteApp(CommandLineApplication app, string[] args)
        {   
            // Support Response File
            foreach(var arg in args)
            {
                if(arg.Contains(".rsp"))
                {
                    args = ParseResponseFile(arg);

                    if (args == null)
                    {
                        return 1;
                    }
                }
            }

            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Console.WriteLine(ex);
#else
                Reporter.Error.WriteLine(ex.Message);
#endif
                return 1;
            }
        }

        private static string[] ParseResponseFile(string rspPath)
        {
            if (!File.Exists(rspPath))
            {
                Reporter.Error.WriteLine("Invalid Response File Path");
                return null;
            }

            string content = null;
            try
            {
                content = File.ReadAllText(rspPath);
            }
            catch (Exception e)
            {
                Reporter.Error.WriteLine("Unable to Read Response File");
                return null;
            }

            string[] nArgs = content.Split('\n');
            return nArgs;
        }

        private static CommandLineApplication SetupApp()
        {
            var app = new CommandLineApplication
            {
                Name = "dotnet compile native",
                FullName = "IL to Native compiler",
                Description = "IL to Native compiler Compiler for the .NET Platform"
            };

            app.HelpOption("-h|--help");

            var managedInputArg = app.Argument("<INPUT_ASSEMBLY>", "The managed input assembly to compile to native.");
            var outputArg = app.Option("-o|--out <OUTPUT_DIR>", "Output Directory for native executable.", CommandOptionType.SingleValue);
            var intermediateArg = app.Option("-t|--temp-output <OUTPUT_DIR>", "Directory for intermediate files.", CommandOptionType.SingleValue);
            var buildConfigArg = app.Option("-c|--configuration <TYPE>", "debug/release build configuration. Defaults to debug.", CommandOptionType.SingleValue);
            var modeArg = app.Option("-m|--mode <MODE>", "Code Generation mode. Defaults to ryujit. ", CommandOptionType.SingleValue);

            var referencesArg = app.Option("-r|--reference <REF_PATH>", "Use to specify Managed DLL references of the app.", CommandOptionType.MultipleValue);
            
            // Custom Extensibility Points to support CoreRT workflow TODO better descriptions
            var ilcArgs = app.Option("--ilcargs <CODEGEN>", "Use to specify custom arguments for the IL Compiler.", CommandOptionType.SingleValue);
            var ilcPathArg = app.Option("--ilcpath <ILC_PATH>", "Use to plug in a custom built ilc.exe", CommandOptionType.SingleValue);
            var linklibArg = app.Option("--linklib <LINKLIB>", "Use to link in additional static libs", CommandOptionType.MultipleValue);
            
            // TEMPORARY Hack until CoreRT compatible Framework Libs are available 
            var appdepSdkPathArg = app.Option("--appdepsdk <SDK>", "Use to plug in custom appdepsdk path", CommandOptionType.SingleValue);
            
            // Optional Log Path
            var logpathArg = app.Option("--logpath <LOG_PATH>", "Use to dump Native Compilation Logs to a file.", CommandOptionType.SingleValue);
            
            app.OnExecute(() =>
            {
                var cmdLineArgs = new ArgValues()
                {
                    InputManagedAssemblyPath = managedInputArg.Value,
                    OutputDirectory = outputArg.Value(),
                    IntermediateDirectory = intermediateArg.Value(),
                    Architecture = "x64",
                    BuildConfiguration = buildConfigArg.Value(),
                    NativeMode = modeArg.Value(),
                    ReferencePaths = referencesArg.Values,
                    IlcArgs = ilcArgs.Value(),
                    IlcPath = ilcPathArg.Value(),
                    LinkLibPaths = linklibArg.Values,
                    AppDepSDKPath = appdepSdkPathArg.Value(),
                    LogPath = logpathArg.Value()
                };

                var config = ParseAndValidateArgs(cmdLineArgs);
                
                DirectoryExtensions.CleanOrCreateDirectory(config.OutputDirectory);
                DirectoryExtensions.CleanOrCreateDirectory(config.IntermediateDirectory);
                
                var nativeCompiler = NativeCompiler.Create(config);
                
                var result = nativeCompiler.CompileToNative(config);

                return result ? 0 : 1;
            });
            
            return app;
        }
        
        private static NativeCompileSettings ParseAndValidateArgs(ArgValues args)
        {
            var config = new NativeCompileSettings();
            
            // Managed Input
            if (string.IsNullOrEmpty(args.InputManagedAssemblyPath) || !File.Exists(args.InputManagedAssemblyPath))
            {
                //TODO make this message good
                throw new Exception("Invalid Managed Assembly Argument.");
            }
            
            config.InputManagedAssemblyPath = Path.GetFullPath(args.InputManagedAssemblyPath);
            
            // Architecture
            if(string.IsNullOrEmpty(args.Architecture))
            {
                config.Architecture = RuntimeExtensions.GetCurrentArchitecture();

                // CoreRT does not support x86 yet
                if (config.Architecture != ArchitectureMode.x64)
                {
                    throw new Exception("Native Compilation currently only supported for x64.");
                }
            }
            else
            {
                try
                {
                    config.Architecture = EnumExtensions.Parse<ArchitectureMode>(args.Architecture.ToLower());
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid Architecture Option.");
                }
            }
            
            // BuildConfiguration 
            if(string.IsNullOrEmpty(args.BuildConfiguration))
            {
                config.BuildType = GetDefaultBuildType();
            }
            else
            {
                try
                {
                    config.BuildType = EnumExtensions.Parse<BuildConfiguration>(args.BuildConfiguration.ToLower());
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid Configuration Option.");
                }
            }
            
            // Output
            if(string.IsNullOrEmpty(args.OutputDirectory))
            {
                config.OutputDirectory = GetDefaultOutputDir(config);
            }
            else
            {
                config.OutputDirectory = args.OutputDirectory;
            }
            
            // Intermediate
            if(string.IsNullOrEmpty(args.IntermediateDirectory))
            {
                config.IntermediateDirectory = GetDefaultIntermediateDir(config);
            }
            else
            {
                config.IntermediateDirectory = args.IntermediateDirectory; 
            }
            
            // Mode
            if (string.IsNullOrEmpty(args.NativeMode))
            {
                config.NativeMode = GetDefaultNativeMode();
            }
            else
            {
                try
                {
                    config.NativeMode = EnumExtensions.Parse<NativeIntermediateMode>(args.NativeMode.ToLower());
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid Mode Option.");
                }
            }

            // AppDeps (TEMP)
            if(!string.IsNullOrEmpty(args.AppDepSDKPath))
            {
                if (!Directory.Exists(args.AppDepSDKPath))
                {
                    throw new Exception("AppDepSDK Directory does not exist.");
                }

                config.AppDepSDKPath = args.AppDepSDKPath;

                var reference = Path.Combine(config.AppDepSDKPath, "*.dll");
                config.ReferencePaths.Add(reference);
            }
            else
            {
                config.AppDepSDKPath = GetDefaultAppDepSDKPath();

                var reference = Path.Combine(config.AppDepSDKPath, "*.dll");
                config.ReferencePaths.Add(reference);
            }

            // IlcPath
            if (!string.IsNullOrEmpty(args.IlcPath))
            {
                if (!Directory.Exists(args.IlcPath))
                {
                    throw new Exception("ILC Directory does not exist.");
                }

                config.IlcPath = args.IlcPath;
            }
            else
            {
                config.IlcPath = GetDefaultIlcPath();
            }

            // logpath
            if (!string.IsNullOrEmpty(args.LogPath))
            {
                config.LogPath = Path.GetFullPath(args.LogPath);
            }

            // CodeGenPath
            if (!string.IsNullOrEmpty(args.IlcArgs))
            {
                config.IlcArgs = Path.GetFullPath(args.IlcArgs);
            }

            // Reference Paths
            foreach (var reference in args.ReferencePaths)
            {
                config.ReferencePaths.Add(Path.GetFullPath(reference));
            }

            // Link Libs
            foreach (var lib in args.LinkLibPaths)
            {
                config.LinkLibPaths.Add(lib);
            }

            // OS
            config.OS = RuntimeInformationExtensions.GetCurrentOS();
            
            return config;
        }

        private static string GetDefaultOutputDir(NativeCompileSettings config)
        {
            var dir = Path.Combine(Constants.BinDirectoryName, config.Architecture.ToString(), config.BuildType.ToString(), "native");

            return Path.GetFullPath(dir);
        }

        private static string GetDefaultIntermediateDir(NativeCompileSettings config)
        {
            var dir = Path.Combine(Constants.ObjDirectoryName, config.Architecture.ToString(), config.BuildType.ToString(), "native");

            return Path.GetFullPath(dir);
        }

        private static BuildConfiguration GetDefaultBuildType()
        {
            return BuildConfiguration.debug;
        }

        private static NativeIntermediateMode GetDefaultNativeMode()
        {
            return NativeIntermediateMode.ryujit;
        }

        private static string GetDefaultAppDepSDKPath()
        {
            var appRoot = AppContext.BaseDirectory;

            var dir = Path.Combine(appRoot, "appdepsdk");

            return dir;
        }

        private static string GetDefaultIlcPath()
        {
            return AppContext.BaseDirectory;
        }
    }
}

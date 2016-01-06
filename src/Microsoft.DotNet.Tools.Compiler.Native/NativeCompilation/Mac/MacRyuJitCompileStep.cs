﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;

namespace Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation.Mac
{
    public class MacRyuJitCompileStep : INativeCompilationComponent
    {
        private readonly string CompilerName = "clang";
        private readonly string InputExtension = ".obj";

        private readonly string CompilerOutputExtension = "";

        // TODO: debug/release support
        private readonly string cflags = "-g -lstdc++ -Wno-invalid-offsetof -pthread -ldl -lm -liconv";

        private readonly string[] IlcSdkLibs = new string[]
        {
            "libbootstrapper.a",
            "libRuntime.a",
            "libSystem.Private.CoreLib.Native.a"
        };

        private readonly string[] appdeplibs = new string[]
        {
            "libSystem.Native.a"
        };


        private string CompilerArgStr { get; set; }
        private NativeCompileSettings config;

        public MacRyuJitCompileStep(NativeCompileSettings config)
        {
            this.config = config;
            InitializeArgs(config);
        }

        public int Invoke()
        {
            var result = InvokeCompiler();
            if (result != 0)
            {
                Reporter.Error.WriteLine("Compilation of intermediate files failed.");
            }

            return result;
        }

        public bool CheckPreReqs()
        {
            // TODO check for clang
            return true;
        }

        private void InitializeArgs(NativeCompileSettings config)
        {
            var argsList = new List<string>();

            // Flags
            argsList.Add(cflags);
            
            // Pass the optional native compiler flags if specified
            if (!string.IsNullOrWhiteSpace(config.CppCompilerFlags))
            {
                argsList.Add(config.CppCompilerFlags);
            }
            
            // Input File
            var inLibFile = DetermineInFile(config);
            argsList.Add("-Xlinker "+inLibFile);

            // ILC SDK Libs
            var IlcSdkLibPath = Path.Combine(config.IlcSdkPath, "sdk");
            foreach (var lib in IlcSdkLibs)
            {
                var libPath = Path.Combine(IlcSdkLibPath, lib);
                argsList.Add("-Xlinker "+libPath);
            }

            // AppDep Libs
            var baseAppDepLibPath = Path.Combine(config.AppDepSDKPath, "CPPSdk/osx.10.10", config.Architecture.ToString());
            foreach (var lib in appdeplibs)
            {
                var appDepLibPath = Path.Combine(baseAppDepLibPath, lib);
                argsList.Add("-Xlinker "+appDepLibPath);
            }

            // Output
            var libOut = DetermineOutputFile(config);
            argsList.Add($"-o \"{libOut}\"");

            this.CompilerArgStr = string.Join(" ", argsList);
        }

        private int InvokeCompiler()
        {
            var result = Command.Create(CompilerName, CompilerArgStr)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();

            return result.ExitCode;
        }

        private string DetermineInFile(NativeCompileSettings config)
        {
            var intermediateDirectory = config.IntermediateDirectory;

            var filename = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);

            var infile = Path.Combine(intermediateDirectory, filename + InputExtension);

            return infile;
        }

        public string DetermineOutputFile(NativeCompileSettings config)
        {
            var intermediateDirectory = config.OutputDirectory;

            var filename = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);

            var outfile = Path.Combine(intermediateDirectory, filename + CompilerOutputExtension);

            return outfile;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation;
using Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation.Common;

namespace Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation.Linux
{
    public class LinuxCppCompileStep : INativeCompilationComponent
    {
        // TODO: debug/release support
        private readonly string[] clangFlags = new string[]
        {
            "-lm", "-ldl", "-g", "-lstdc++", "-lrt", "-Wno-invalid-offsetof", "-pthread"
        };

        private readonly string[] ilcLibs = new string[]
        {
            "libbootstrappercpp.a",
            "libPortableRuntime.a",
            "libSystem.Private.CoreLib.Native.a"
        };

        private readonly string[] sdkLibs = new string[]
        {
            "CPPSdk/ubuntu.14.04/x64/libSystem.Native.a"
        };

        private readonly string[] sdkIncludePaths = new string[]
        {
            "CPPSdk/ubuntu.14.04",
            "CPPSdk"
        };

        private readonly string[] sdkInputFiles = new string[]
        {
            "CPPSdk/ubuntu.14.04/lxstubs.cpp"
        };

        
        private NativeCompileSettings _config;
        private IEnumerable<string> _inputCppFiles;
        private INativeCompilationComponent _compilerComponent;
        private string _outputFilePath;

        public LinuxCppCompileStep(NativeCompileSettings config, IEnumerable<string> inputCppFiles, string outputFilePath)
        {
            this._config = config;
            this._inputCppFiles = inputCppFiles;
            this._outputFilePath = outputFilePath;

            this._compilerComponent = ConstructCompilerComponent();
        }

        public int Invoke()
        {
            var result = _compilerComponent.Invoke();
            if (result != 0)
            {
                Reporter.Error.WriteLine("Linux Cpp Compilation Step Failed");
            }

            return result;
        }

        public bool CheckPreReqs()
        {
            return _compilerComponent.CheckPreReqs();
        }

        private INativeCompilationComponent ConstructCompilerComponent()
        {
            var inputFiles = ConstructInputFiles();
            var includePaths = ConstructIncludePaths();

            var compiler = new ClangComponent(inputFiles,
                                              includePaths,
                                              clangFlags,
                                              _outputFilePath);

            return compiler;
        }

        private IEnumerable<string> ConstructInputFiles()
        {
            var inputFiles = new List<string>();
            inputFiles.AddRange(_inputCppFiles);

            foreach (var lib in ilcLibs)
            {
                inputFiles.Add(Path.Combine(_config.IlcPath, lib));
            }
            
            foreach (var lib in sdkLibs)
            {
                inputFiles.Add(Path.Combine(_config.AppDepSDKPath, lib));
            }

            return inputFiles;
        }

        private IEnumerable<string> ConstructIncludePaths()
        {
            var includePaths = new List<string>();

            foreach (var include in sdkIncludePaths)
            {
                includePaths.Add(Path.Combine(_config.AppDepSDKPath, include));
            }

            return includePaths;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;

namespace Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation
{
    public class ILCompilerInvoker : INativeCompilationComponent
    {
        private readonly string ExecutableName = "corerun" + Constants.ExeSuffix;
        private readonly string ILCompiler = "ilc.exe";

        private static readonly Dictionary<NativeIntermediateMode, string> ModeOutputExtensionMap = new Dictionary<NativeIntermediateMode, string>
        {
            { NativeIntermediateMode.cpp, ".cpp" },
            { NativeIntermediateMode.ryujit, ".obj" }
        };

        private string ArgStr { get; set; }
        private NativeCompileSettings _config;
        private string _outputFile;
        
        public ILCompilerInvoker(NativeCompileSettings config, string outputFile)
        {
            this._config = config;
            this._outputFile = outputFile;

            InitializeArgs(config);
        }
        
        private void InitializeArgs(NativeCompileSettings config)
        {
            var argsList = new List<string>();

            var ilcPath = Path.Combine(config.IlcPath, ILCompiler);
            if (!File.Exists(ilcPath))
            {
                throw new FileNotFoundException("Unable to find ILCompiler at " + ilcPath);
            }

            argsList.Add($"\"{ilcPath}\"");
            
            // Input File 
            var inputFilePath = config.InputManagedAssemblyPath;
            argsList.Add($"\"{inputFilePath}\"");
            
            // System.Private.CoreLib Reference
            var coreLibPath = Path.Combine(config.IlcSdkPath, "sdk", "System.Private.CoreLib.dll");
            argsList.Add($"-r \"{coreLibPath}\"");
            
            // AppDep References
            foreach (var reference in config.ReferencePaths)
            {
                argsList.Add($"-r \"{reference}\"");
            }
            
            // Set Output
            argsList.Add($"-out \"{_outputFile}\"");
            
            // Mode Flat
            if (config.NativeMode == NativeIntermediateMode.cpp)
            {
                argsList.Add("-cpp");
            }
            
            // Custom Ilc Args support
            if (! string.IsNullOrEmpty(config.IlcArgs))
            {
                argsList.Add(config.IlcArgs);
            }
                        
            this.ArgStr = string.Join(" ", argsList);
        }
        
        public int Invoke()
        {
            var executablePath = Path.Combine(_config.IlcPath, ExecutableName);
            
            var result = Command.Create(executablePath, ArgStr)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();
            
            return result.ExitCode;
        }

        public bool CheckPreReqs()
        {
            var ilcPath = Path.Combine(_config.IlcPath, ILCompiler);
            return File.Exists(ilcPath);
        }
    }
}

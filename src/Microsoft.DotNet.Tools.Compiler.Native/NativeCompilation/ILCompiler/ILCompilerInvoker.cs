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
        private readonly string HostExecutable = "corerun" + Constants.ExeSuffix;
        private readonly string ILCompilerExecutable = "ilc.exe";

        private static readonly Dictionary<NativeIntermediateMode, string> s_modeExtensionMap = new Dictionary<NativeIntermediateMode, string>
        {
            { NativeIntermediateMode.cpp, ".cpp" },
            { NativeIntermediateMode.ryujit, ".obj" }
        };
        
        private NativeCompileSettings _config;
        private ILCompiler _ilCompiler;
        private string _outputFile;
        private string _argStr;

        public static ILCompilerInvoker Create(NativeCompileSettings config)
        {
            return new ILCompilerInvoker(config, DetermineOutputFile(config));
        }

        public static string DetermineOutputFile(NativeCompileSettings config)
        {
            var inputAssemblyName = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);
            var outputExtension = s_modeExtensionMap[config.NativeMode];

            var outputFilePath = Path.Combine(config.IntermediateDirectory, inputAssemblyName, outputExtension);

            return outputFilePath;
        }

        public ILCompilerInvoker(NativeCompileSettings config, string outputFile)
        {
            this._config = config;
            this._outputFile = outputFile;

            Initialize(config);
        }
        
        private void Initialize(NativeCompileSettings config)
        {
            var ilcExecutablePath = Path.Combine(config.IlcPath, ILCompilerExecutable);
            var coreLibPath = Path.Combine(config.IlcSdkPath, "sdk", "System.Private.CoreLib.dll");

            List<string> references = new List<string>();
            references.Add(coreLibPath);
            references.AddRange(config.ReferencePaths);

            _ilCompiler = new ILCompiler(
                config.InputManagedAssemblyPath,
                _outputFile,
                config.NativeMode == NativeIntermediateMode.cpp,
                config.IlcArgs ?? "",
                references
                );

            _argStr = $"\"{ilcExecutablePath}\" {_ilCompiler.BuildArgumentString()}";
        }
        
        public int Invoke()
        {
            var executablePath = Path.Combine(_config.IlcPath, HostExecutable);
            
            var result = Command.Create(executablePath, _argStr)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();
            
            return result.ExitCode;
        }

        public bool CheckPreReqs()
        {
            var ilcExecutablePath = Path.Combine(_config.IlcPath, ILCompilerExecutable);

            if (!File.Exists(ilcExecutablePath))
            {
                Reporter.Error.WriteLine("ILCompiler Not found at: " + ilcExecutablePath);
                return false;
            }

            return true;
        }
    }
}

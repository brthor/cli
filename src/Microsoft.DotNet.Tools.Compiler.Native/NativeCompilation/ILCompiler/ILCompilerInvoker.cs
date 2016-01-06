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
            var inputAssemblyName = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);
            var outputExtension = s_modeExtensionMap[config.NativeMode];

            var outputFilePath = Path.Combine(config.IntermediateDirectory, inputAssemblyName, outputExtension);

            return new ILCompilerInvoker(config, outputFilePath);
        }

        public ILCompilerInvoker(NativeCompileSettings config, string outputFile)
        {
            this._config = config;
            this._outputFile = outputFile;

            Initialize(config);
        }
        
        private void Initialize(NativeCompileSettings config)
        {
            var ilcPath = Path.Combine(config.IlcPath, ILCompiler);
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

            _argStr = $"{ilcPath} {_ilCompiler.BuildArgumentString()}";
        }
        
        public int Invoke()
        {
            var executablePath = Path.Combine(_config.IlcPath, ExecutableName);
            
            var result = Command.Create(executablePath, _argStr)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();
            
            return result.ExitCode;
        }

        public bool CheckPreReqs()
        {
            var ilcPath = Path.Combine(_config.IlcPath, ILCompiler);

            if (!File.Exists(ilcPath))
            {
                Reporter.Error.WriteLine("ILCompiler Not found at: " + ilcPath);
                return false;
            }

            return true;
        }
    }
}

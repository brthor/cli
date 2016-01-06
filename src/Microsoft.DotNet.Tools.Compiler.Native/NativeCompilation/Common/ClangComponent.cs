using System;
using System.Collections.Generic;

using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation;
using System.Text;

namespace Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation.Common
{
    public class ClangComponent : INativeCompilationComponent
    {
        private readonly string ComponentExecutable = "clang-3.5";

        private string _argStr;
        private IEnumerable<string> _orderedInputFiles;
        private IEnumerable<string> _includePaths;
        private IEnumerable<string> _argFlags;
        private string _outputFilePath;

        public ClangComponent(IEnumerable<string> orderedInputFiles,
                              IEnumerable<string> includePaths,
                              IEnumerable<string> argFlags,
                              string outputFilePath)
        {
            this._orderedInputFiles = orderedInputFiles;
            this._includePaths = includePaths;
            this._argFlags = argFlags;
            this._outputFilePath = outputFilePath;

            this._argStr = ConstructArgStr();
        }
        

        public int Invoke()
        {
            var result = Command.Create(ComponentExecutable, _argStr)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute()
                .ExitCode;

            if (result != 0)
            {
                Reporter.Error.WriteLine("Clang Invocation Failed");
            }

            return result;
        }

        public bool CheckPreReqs()
        {
            try
            {
                var result = Command.Create(ComponentExecutable, "--help")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .ExitCode;

                return result == 0;
            } catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e);
#endif
                return false;
            }
            
        }

        private string ConstructArgStr()
        {
            var argList = new List<string>();

            if (_argFlags != null)
            {
                foreach (var flag in _argFlags)
                {
                    argList.Add(flag);
                }
            }

            if (_includePaths != null)
            {
                foreach (var include in _includePaths)
                {
                    argList.Add($"-I \"{include}\"");
                }
            }

            if (_orderedInputFiles != null)
            {
                foreach (var input in _orderedInputFiles)
                {
                    argList.Add($"\"{input}\"");
                }
            }
            
            if (_outputFilePath != null)
            {
                argList.Add($"-o \"{_outputFilePath}\"");
            }

            return string.Join(" ", argList);
        }
    }
}
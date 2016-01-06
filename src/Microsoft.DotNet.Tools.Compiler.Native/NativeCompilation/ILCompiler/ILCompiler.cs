using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation
{
    public class ILCompiler
    {
        private string _inputFilePath;
        private string _outputFilePath;
        private bool _cppMode;
        private string _ilcArgs;
        private List<string> _references;

        private string OutputFilePathOption
        {
            get
            {
                return _outputFilePath == string.Empty ?
                                          "" :
                                          $"-out {_outputFilePath}";
            }
        }

        private string CppModeOption
        {
            get
            {
                return _cppMode ?
                    $"-cpp" :
                    "";
            }
        }

        private string ReferencesOptions
        {
            get
            {
                string referenceOptions = string.Empty;

                foreach (var reference in _references)
                {
                    referenceOptions += $"-r {reference} ";
                }

                return referenceOptions;
            }
        }

        public ILCompiler(
            string inputFilePath,
            string outputFilePath="",
            bool cppMode=false,
            string ilcArgs="",
            List<string> references=null)
        {
            _inputFilePath = inputFilePath;
            _outputFilePath = outputFilePath;
            _cppMode = cppMode;
            _ilcArgs = ilcArgs;
            _references = references ?? new List<string>();
        }

        public string BuildArgumentString()
        {
            return $"{_inputFilePath} {_ilcArgs} {OutputFilePathOption} {CppModeOption} {ReferencesOptions}";
        }
    }
}

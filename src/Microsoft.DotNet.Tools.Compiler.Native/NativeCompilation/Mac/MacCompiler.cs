using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;

namespace Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation.Mac
{
    public class MacCompiler : INativeCompilationComponent
    {
        public static MacCompiler Create(NativeCompileSettings config, string inputFile)
        {
            var stepComponents = SelectStepComponentsForMode(config, inputFile, DetermineOutputFile(config));

            var platformCompiler = new MacCompiler()
            {
                Components = stepComponents
            };

            return platformCompiler;
        }

        public static string DetermineOutputFile(NativeCompileSettings config)
        {
            var inputAssemblyName = Path.GetFileNameWithoutExtension(config.InputManagedAssemblyPath);
            var outputFilePath = Path.Combine(config.OutputDirectory, inputAssemblyName);
            return outputFilePath;
        }

        private static IEnumerable<INativeCompilationComponent> SelectStepComponentsForMode(NativeCompileSettings config, string inputFile, string outputFile)
        {
            var stepComponents = new List<INativeCompilationComponent>();

            if (config.NativeMode == NativeIntermediateMode.cpp)
            {
                var step = new MacCppCompileStep(config, new string[] { inputFile }, outputFile);
                stepComponents.Add(step);
            }
            else if (config.NativeMode == NativeIntermediateMode.ryujit)
            {
                var step = new MacRyuJitCompileStep(config, new string[] { inputFile }, outputFile);
                stepComponents.Add(step);
            }
            else
            {
                throw new Exception("Unsupported Native Compilation Mode on Mac: " + config.NativeMode.ToString());
            }

            return stepComponents;
        }

        private IEnumerable<INativeCompilationComponent> Components { get; set; }

        public int Invoke()
        {
            foreach (var component in Components)
            {
                var result = component.Invoke();

                if (result != 0)
                {
                    Reporter.Error.WriteLine("Mac Compilation Step Failed: " + component.GetType().Name);
                    return result;
                }
            }

            return 0;
        }

        public bool CheckPreReqs()
        {
            foreach (var component in Components)
            {
                if (!component.CheckPreReqs()) return false;
            }
            return true;
        }
    }
}
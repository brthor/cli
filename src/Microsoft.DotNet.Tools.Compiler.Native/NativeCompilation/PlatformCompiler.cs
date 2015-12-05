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
    public class PlatformCompiler : INativeCompilationComponent
    {
        public static PlatformCompiler Create(NativeCompileSettings config)
        {
            var platformComponent = SelectPlatformCompilerForOS(config);
            
            var pc = new PlatformCompiler() 
            {
                Components = new List<INativeCompilationComponent>
                {
                    platformComponent
                }
            };
            
            return pc;
        }

        private static INativeCompilationComponent SelectPlatformCompilerForOS(NativeCompileSettings config)
        {
            if (config.OS == OSMode.Windows)
            {
                return WindowsCompiler.Create(config);
            }
            else if (config.OS == OSMode.Linux)
            {
                return LinuxCompiler.Create(config);
            }
            else if (config.OS == OSMode.Mac)
            {
                return MacCompiler.Create(config);
            }
            else
            {
                throw new Exception("Unrecognized OS: " + config.OS.ToString());
            }
        }
        
        private List<INativeCompilationComponent> Components { get; set; }

        public int Invoke()
        {
            foreach (var component in Components)
            {
                var result = component.Invoke();

                if (result != 0)
                {
                    Reporter.Error.WriteLine("Platform Compilation Component Failed: " + component.GetType().Name);
                    return result;
                }
            }

            return 0;
        }
        
        public bool CheckPreReqs()
        {
            foreach(var component in Components)
            {
                if (!component.CheckPreReqs()) return false;
            }
            return true;
        }
    }
}
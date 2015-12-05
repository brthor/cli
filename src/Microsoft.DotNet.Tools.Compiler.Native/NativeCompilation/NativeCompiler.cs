
using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Compiler.Native;

namespace Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation
{
    public class NativeCompiler : INativeCompilationComponent
    {
        public static NativeCompiler Create(NativeCompileSettings config)
        {
            var nc = new NativeCompiler() 
            {
                Components = new List<INativeCompilationComponent>()
            };

            nc.Components.Add(new ILCompilerInvoker(config));
            nc.Components.Add(PlatformCompiler.Create(config));
            
            return nc;
        }

        private List<INativeCompilationComponent> Components { get; set; };

        public int Invoke()
        {
            foreach (var component in Components)
            {
                var result = component.Invoke();

                if (result != 0)
                {
                    Reporter.Error.WriteLine("Native Compilation Component Failed: " + component.GetType().Name);
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
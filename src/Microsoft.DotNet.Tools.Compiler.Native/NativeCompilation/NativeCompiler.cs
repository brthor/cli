
using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Compiler.Native.NativeCompilation
{
    public class NativeCompiler : INativeCompilationComponent
    {
        public static NativeCompiler Create(NativeCompileSettings config)
        {
            var nativeCompiler = new NativeCompiler() 
            {
                Components = new List<INativeCompilationComponent>()
            };

            nativeCompiler.Components.Add(ILCompilerInvoker.Create(config));
            nativeCompiler.Components.Add(PlatformCompiler.Create(config));
            
            return nativeCompiler;
        }

        private List<INativeCompilationComponent> Components { get; set; }

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
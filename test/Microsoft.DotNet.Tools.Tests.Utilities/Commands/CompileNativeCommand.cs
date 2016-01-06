// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class CompileNativeCommand : TestCommand
    {
        private string _inputAssembly;
        private string _outputDirectory;
        private string _tempOutputDirectory;
        private string _configuration;
        private string _mode;
        private string _ilcArgs;
        private string _ilcPath;
        private string _ilcSdkPath;
        private string _appDepSdk;
        private string _logPath;
        private IEnumerable<string> _linkLibs;
        private IEnumerable<string> _references;
        private string _cppCompilerFlags;
        private bool _help;

        private string OutputOption
        {
            get
            {
                return string.IsNullOrEmpty(_outputDirectory) ?
                                           "" :
                                           $"--output {_outputDirectory}";
            }
        }

        private string TempOutputOption
        {
            get
            {
                return string.IsNullOrEmpty(_tempOutputDirectory) ?
                                           "" :
                                           $"--temp-output {_tempOutputDirectory}";
            }
        }

        private string ConfigurationOption
        {
            get
            {
                return string.IsNullOrEmpty(_configuration) ?
                                           "" :
                                           $"--configuration {_configuration}";
            }
        }

        private string ModeOption
        {
            get
            {
                return string.IsNullOrEmpty(_mode) ?
                                           "" :
                                           $"--mode {_mode}";
            }
        }

        private string IlcArgsOption
        {
            get
            {
                return string.IsNullOrEmpty(_ilcArgs) ?
                                           "" :
                                           $"--ilcargs {_ilcArgs}";
            }
        }

        private string IlcPathOption
        {
            get
            {
                return string.IsNullOrEmpty(_ilcPath) ?
                                           "" :
                                           $"--ilcpath {_ilcPath}";
            }
        }

        private string IlcSdkPathOption
        {
            get
            {
                return string.IsNullOrEmpty(_ilcSdkPath) ?
                                           "" :
                                           $"--ilcsdkpath {_ilcSdkPath}";
            }
        }

        private string AppDepSdkOption
        {
            get
            {
                return string.IsNullOrEmpty(_appDepSdk) ?
                                           "" :
                                           $"--appdepsdk {_appDepSdk}";
            }
        }

        private string LinkLibsOptions #todo
        {
            get
            {
                string referenceOptions = string.Empty;

                foreach (var linklib in _linkLibs)
                {
                    referenceOptions += $"--linklib {reference} ";
                }

                return referenceOptions;
            }
        }

         private string ReferencesOptions
        {
            get
            {
                string referenceOptions = string.Empty;

                foreach (var reference in _references)
                {
                    referenceOptions += $"--reference {reference} ";
                }

                return referenceOptions;
            }
        }

        private string CppCompilerFlagsOption
        {
            get
            {
                return string.IsNullOrEmpty(_cppCompilerFlags) ?
                                           "" :
                                           $"--cppcompilerflags {_configuration}";
            }
        }

        private string HelpOption
        {
            get
            {
                return _help ?
                    $"--help":
                    "";
            }
        }

        public CompileNativeCommand(
            string projectPath, 
            string output="", 
            string tempOutput="", 
            string configuration="",
            string mode="",
            string ilcArgs="",
            string ilcPath="",
            string ilcSdkPath="",
            string appDepSdk="",
            string logPath="",
            IEnumerable<string> linkLibs=null,
            IEnumerable<string> references=null,
            string cppCompilerFlags="",
            bool help=false)
            : base("dotnet")
        {
            _projectPath = projectPath;
            _outputDirectory = output;
            _tempOutputDirectory = tempOutput;
            _configuration = configuration;
            _mode = mode;
            _ilcArgs = ilcArgs;
            _ilcPath = ilcPath;
            _ilcSdkPath = ilcSdkPath;
            _appDepSdk = appDepSdk;
            _logPath = logPath;
            _linkLibs = linkLibs ?? new List<string>();
            _references = references ?? new List<string>();
            _cppCompilerFlags = cppcompilerflags;
            _help = help;
        }

        public override CommandResult Execute(string args = "")
        {
            args = $"compile-native {BuildArgs()} {args}";
            return base.Execute(args);
        }

        private string BuildArgs()
        {
            return $"{_projectPath} {OutputOption} {TempOutputOption} {ConfigurationOption} {VersionSuffixOption}";
        }
    }
}

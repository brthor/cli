﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Archive;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Configurer
{
    public class NuGetPackagesArchiver : INuGetPackagesArchiver
    {
        private ITemporaryDirectory _temporaryDirectory;

        public string NuGetPackagesArchive => DotnetFiles.NuGetPackagesArchive;
        
        public NuGetPackagesArchiver() : this(FileSystemWrapper.Default.Directory)
        {            
        }

        internal NuGetPackagesArchiver(IDirectory directory)
        {
            _temporaryDirectory = directory.CreateTemporaryDirectory();
        }

        public string ExtractArchive()
        {        
            var progress = new ConsoleProgressReport();
            var archive = new IndexedArchive();

            archive.Extract(NuGetPackagesArchive, _temporaryDirectory.DirectoryPath, progress);

            return _temporaryDirectory.DirectoryPath;
        }

        public void Dispose()
        {
            _temporaryDirectory.Dispose();
        }        
    }
}
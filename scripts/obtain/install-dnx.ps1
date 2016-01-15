#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

. $PSScriptRoot\..\common\_common.ps1

$DnxFlavor = "dnx-coreclr-win-x64"
$DnxVersion = "1.0.0-rc2-16390"

$doInstall = $true

# check if the required dnx version is already downloaded
if ((Test-Path "$DnxRoot\dnx.exe")) {
    $dnxOut = & "$DnxRoot\dnx.exe" --version

    if ($dnxOut -Match $DnxVersion) {
        Write-Host "Dnx version - $DnxVersion already downloaded."
        
        $doInstall = $false
    }
}

if ($doInstall)
{
    # Download dnx to copy to stage2
    Remove-Item -Recurse -Force -ErrorAction Ignore $DnxDir
    mkdir -Force "$DnxDir" | Out-Null

    Write-Host "Downloading Dnx version - $DnxVersion."
    $DnxUrl="https://www.myget.org/F/aspnetvolatiledev/api/v2/package/$DnxFlavor/$DnxVersion"
    Invoke-WebRequest -UseBasicParsing "$DnxUrl" -OutFile "$DnxDir\dnx.zip"

    Add-Type -Assembly System.IO.Compression.FileSystem | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory("$DnxDir\dnx.zip", "$DnxDir")
}
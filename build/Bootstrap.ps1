param(
    [Parameter(Mandatory = $false)]
    [switch]$Quiet
)

if($env:LORDMILKO_BUILDTOOLS_DEVELOPMENT)
{
    switch($env:LORDMILKO_BUILDTOOLS_DEVELOPMENT)
    {
        "SelfBootstrap" {
            dotnet build $PSScriptRoot\..\BuildTools\BuildTools.csproj -c Release

            Import-Module $PSScriptRoot\..\BuildTools\bin\Release\net461\lordmilko.BuildTools
        }

        "AppveyorArtifact" {
            if(!(Get-Command New-BuildEnvironment -ErrorAction SilentlyContinue))
            {
                $bytes = (Invoke-WebRequest https://ci.appveyor.com/api/projects/lordmilko/buildtools/artifacts/lordmilko.BuildTools.zip -UseBasicParsing).Content

                $stream = New-Object System.IO.MemoryStream (,$bytes)

                if($PSEdition -eq "Desktop")
                {
                    Add-Type -AssemblyName System.IO.Compression
                }

                $archive = [System.IO.Compression.ZipArchive]::new($stream)

                $dllEntry = $archive.Entries|where Fullname -eq "fullclr/BuildTools.dll"

                $entryStream = $dllEntry.Open()

                try
                {
                    $entryMemStream = New-Object System.IO.MemoryStream
                    $entryStream.CopyTo($entryMemStream)

                    $dllBytes = $entryMemStream.ToArray()

                    $assembly = [System.Reflection.Assembly]::Load($dllBytes)

                    Import-Module $assembly
                }
                finally
                {
                    $entryStream.Dispose()
                }
            }
        }
    }
}
elseif(!(Get-Module lordmilko.BuildTools))
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    if(!(Get-Module -ListAvailable lordmilko.BuildTools))
    {
        Install-Package lordmilko.BuildTools -ForceBootstrap -Force -Source PSGallery | Out-Null
    }
    
    Import-Module lordmilko.BuildTools -Scope Local
}

Start-BuildEnvironment $PSScriptRoot -CI:(!!$env:CI) -Quiet:$Quiet
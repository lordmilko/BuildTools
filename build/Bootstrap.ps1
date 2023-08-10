param(
    [Parameter(Mandatory = $false)]
    [switch]$Quiet
)

dotnet build $PSScriptRoot\..\BuildTools\BuildTools.csproj -c Release

Import-Module $PSScriptRoot\..\BuildTools\bin\Release\net461\lordmilko.BuildTools

if(!$env:LORDMILKO_BUILDTOOLS_DEVELOPMENT -and !(Get-Module lordmilko.BuildTools))
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    if(!(Get-Module -ListAvailable lordmilko.BuildTools))
    {
        Install-Package lordmilko.BuildTools -ForceBootstrap -Force -Source PSGallery | Out-Null
    }
    
    Import-Module lordmilko.BuildTools -Scope Local
}

Start-BuildEnvironment $PSScriptRoot -CI:(!!$env:CI) -Quiet:$Quiet
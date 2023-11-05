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
    }
}
elseif(!(Get-Module lordmilko.BuildTools))
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    if(!(Get-Module -ListAvailable lordmilko.BuildTools))
    {
        Write-Host "Installing lordmilko.BuildTools..." -NoNewline -ForegroundColor Green

        Register-PackageSource -Name AppveyorBuildToolsNuGet -Location https://ci.appveyor.com/nuget/buildtools-j7nyox2i4tis -ProviderName PowerShellGet | Out-Null

        Install-Package lordmilko.BuildTools -ForceBootstrap -Force -Source AppveyorBuildToolsNuGet | Out-Null

        Unregister-PackageSource -Name AppveyorBuildToolsNuGet

        Write-Host "Done!"
    }
    
    Import-Module lordmilko.BuildTools -Scope Local
}

Start-BuildEnvironment $PSScriptRoot -CI:(!!$env:CI) -Quiet:$Quiet
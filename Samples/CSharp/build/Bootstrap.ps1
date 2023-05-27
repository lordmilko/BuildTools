if(!$env:LORDMILKO_BUILDTOOLS_DEVELOPMENT)
{
	[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

	if(!(Get-Module -ListAvailable lordmilko.BuildTools))
	{
		Install-Package lordmilko.BuildTools -ForceBootstrap -Force | Out-Null
	}

	Import-Module lordmilko.BuildTools -Scope Local
}

Start-BuildEnvironment $PSScriptRoot
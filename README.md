# BuildTools

*BuildTools* is a PowerShell module that allows dynamically generating PowerShell based Build Environments for managing the lifecycle of projects both in and outside of CI.

*BuildTools* provides facilities for managing all of the following lifecycle components

* Build
* Clean
* Coverage
* Packaging
* Testing
* Versioning
* Dependencies
* Analysis
* CI

```powershell
Register-PackageSource -Name AppveyorBuildToolsNuGet -Location https://ci.appveyor.com/nuget/buildtools-j7nyox2i4tis -ProviderName PowerShellGet

Install-Package lordmilko.BuildTools -Source AppveyorBuildToolsNuGet
```

For more information please see the [wiki](https://github.com/lordmilko/BuildTools/wiki)
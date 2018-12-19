# CS-Script.Core
.NET Core port of CS-Script

### Limitations

#### Imposed by .NET Core:
  - No support for script app.config file
    Support for custom app.config files is not available for .NET Core due to the API limitations
  - No building "*.exe"
  - No NuGet inter-package dependencies resolving. All packages (including dependency packages) must be specified in the script
  - Serious compilation start-up delay (.NET Core offers no VBCSCompiler.exe optimisation)<br>
    The indirect signs are indicating that MS is working on this problem. Thus there may be some hope that it will be solved eventually as it was done for Roslyn compiler for .NET Full.
    Where a call "dotnet build ..." forks an addition long standing "hot" process that dispatches all consecutive build requests extremely quickly.

#### CS-Script obsolete features and limitations:
  - All scripts are compiled with debug symbols generated
  - No surrogate process support (`//css_host`) for x86 vs. x64 execution
    _The CPU-specific engine executable must be used._
  - No support for deprecated settings:
    - Settings.DefaultApartmentState 
    - Settings.UsePostProcessor
    - Settings.TargetFramework
    - Settings.CleanupShellCommand
    - Settings.DoCleanupAfterNumberOfRuns
  - No automatic elevation via arg '-elevate'<br>
    _Elevation must be done via shell_

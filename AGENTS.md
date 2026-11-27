# AmbiBack PC - Agent Guide

AmbiBack PC is a C# .NET Framework WinForms app (`AmbiBack.PC.sln`). It captures the Windows desktop with DXGI desktop duplication and streams the colors to AmbiBack controllers and other lights (Hue, WLED, Govee, OpenRGB, Nanoleaf, LIFX).

## Versions and releases

- **The version in the repository is the next version, still in progress.** It is set in `Properties/AssemblyInfo.cs` (`AssemblyVersion` and `AssemblyFileVersion`, e.g. `1.1.2.0`).
- **Every build of that version is an intermediate version.** CI (`.github/workflows/ci.yml`) and local builds use the first three parts from `AssemblyInfo.cs`; the fourth part is the CI run number (or `-BuildNumber` locally). For example, `1.1.2.17` is the 17th intermediate build of 1.1.2.
- **A version is only final when its tag `v<version>` is pushed** (e.g. `v1.1.2`). The tag starts `.github/workflows/release.yml`, which builds, runs the Hue self test and publishes the GitHub release with the setup and the portable ZIP.
- "Make version X" means: set `AssemblyInfo.cs` to X. Push the tag only when the user asks for the release.
- After a release, set `AssemblyInfo.cs` to the next version when the user asks for it.
- Pushing an existing tag again (force) rebuilds and replaces that release. Do this only when the user asks for it.

## Build and test

- Release package: `installer\build-installer.ps1 -Version <x.y.z> -BuildNumber <n> -OutRoot <folder>`. It builds with warnings as errors, runs `AmbiBack.exe --hue-selftest`, and creates the setup and the ZIP. `AssemblyInfo.cs` is patched only for that build and restored afterwards.
- Quick self test of any build: `AmbiBack.exe --hue-selftest "<report.txt>"`. Exit code 0 means pass.
- Only one instance of the app runs. Ask the user to close the installed app before GUI tests; never kill it.

## Logs

- Release builds write errors and events to `ambiback.log` in the data folder: the program folder when it is writable, otherwise `%LOCALAPPDATA%\AmbiBack\PC`. The log is moved to `.old` at 1 MB.
- `CGlobals.LogEvent(...)` writes events that belong in that log. `CGlobals.DebugOutput(...)` is debug output only; its facility "Error" (exceptions) also reaches the log.
- Expected failures, such as COM ports that are not controllers, must not be logged as errors.

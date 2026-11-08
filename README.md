# SeneaLUI–PoVSkills

Source for the unofficial SeneaL UI bridge for Path of Valheiman. Version 2.4.1 includes the shared quiver slot, the tested PoV quiver placement defaults, and the original PoV Halls pin art. See the [package README](package/README.md) for features and installation.

## Build on Windows

Install the .NET SDK and the .NET Framework 4.8 targeting pack. Create a local lib directory and copy the DLLs listed in [the project file](src/SeneaLUI.PovSkills.csproj) from your installed game and mods. The project resolves PathOfValheiman.dll from the default Gale PoVPlus profile path. Adjust that reference locally if your installation differs.

Run dotnet build src/SeneaLUI.PovSkills.csproj -c Release. Package the resulting SeneaLUI.PovSkills.dll at the ZIP root alongside the files in package/.

# SeneaLUI–PoVSkills

An unofficial bridge between SeneaL UI and Path of Valheiman. See the [package README](package/README.md) for features and installation.

## Build it yourself

Build on Windows with the .NET SDK and the .NET Framework 4.8 Developer Pack. The 2.4.1 build was checked with SeneaL UI 1.2.8 and Path of Valheiman 6.0.1.

1. Clone the repository and open PowerShell in its root. To build the exact release source, run `git checkout v2.4.1`.
2. Create a `lib` directory at the repository root. Copy the following DLLs from your local installations.

   | From | DLLs |
   | --- | --- |
   | Valheim’s `valheim_Data/Managed` | `assembly_valheim.dll`, `assembly_guiutils.dll`, `assembly_utils.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.InputLegacyModule.dll`, `UnityEngine.UIModule.dll`, `Unity.TextMeshPro.dll` |
   | Your profile’s `BepInEx/core` | `BepInEx.dll`, `0Harmony.dll` |
   | SeneaL UI under `BepInEx/plugins` | `SeneaL-UI.dll` |

3. The [project file](src/SeneaLUI.PovSkills.csproj) references `PathOfValheiman.dll` directly from the Gale `PoVPlus` profile. If your profile has a different name or location, update that reference to point to your installed DLL.
4. Build and package from the repository root.

   ```powershell
   dotnet build src/SeneaLUI.PovSkills.csproj -c Release
   if ($LASTEXITCODE -ne 0) { throw "Bridge build failed." }

   Compress-Archive -Path @(
       'src/bin/Release/net48/SeneaLUI.PovSkills.dll'
       'package/manifest.json'
       'package/README.md'
       'package/icon.png'
   ) -DestinationPath 'SeneaLUI_PovSkills-2.4.1.zip' -Force
   ```

5. Import the ZIP as a local mod in Gale or r2modman. The DLL, manifest, README and icon should sit at the ZIP root. Keep the reference DLLs in `lib` for local builds.

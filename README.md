# Vokun Mod Manager
Vokun is a semi-automatic mod manager to help you install and organize mods for Skyrim Special Edition (Steam version) by using skse64_loader.exe as the main launcher both on Linux and Windows. It can install files from archive straight to the Data folder and instantly enable mods (NOT ALL OF THEM).
If you were looking for something like Mod Organizer 2 or Vortex Mod Manager that works on Linux, this is not the place. Instead, check out [this repository](https://github.com/SulfurNitride/NaK) by [@SulfurNitride](https://github.com/SulfurNitride).

## Installation

### Before you use it
Before installing the manager, make sure you have [Skyrim Script Extender](https://skse.silverlock.org/) installed first. Add `skse64_loader.exe` to your steam library as a non-steam game.
> [!WARNING]
> Add `skse64_loader.exe` to your steam library only if you are on Linux. On Windows you don't need this, the app will automatically detect the launcher.

Set the proton version (I personally used `Proton  10.0-4`), and in the
launch options add the next line:
``````
STEAM_COMPAT_DATA_PATH=/home/<your-username>/.local/share/Steam/steamapps/compatdata/489830 %command%
``````
This will make the launcher use the original game's files.

### Installing

Just download the latest release from the [release pages](https://github.com/E1ecTro5/vokun-mod-manager/releases). Yeah, this is a portable software with the archive of its dependencies, no need to install, just download and run <ins>**VokunModManager**</ins> file in it. It would be better if the executable remains in folder, since `config.txt` file will appear next to it.

## How to use

### So, here is the UI design:
<img width="800" height="401" alt="image" src="assets/preview1.gif" />

* **`Current mod list`** - on the left side is the list of mods (.esp/.esm/.esl) mentioned in the `Plugins.txt` file. The checkbox represents the `*` symbol in a string, saying whether the mod is currently on or off.
You're also able to reorder them by drag-and-dropping, thanks to [@aldelaro5](https://github.com/aldelaro5) for the [solution](https://github.com/AvaloniaUI/Avalonia/discussions/10877).
> [!NOTE]
> Note that if you disable some of the mods and load a save with the disabled mod included, that mod will automatically turn on

* **`Directories and files`** - just some info about:
  + **`Launcher ID`** - `skse64_loader.exe`'s steam ID to launch it properly on Linux (ignore on Windows).
  + **`shortcuts.vdf`** file - this file is needed to calculate the loader's ID properly (ignore on Windows).
  + **`Game folder`** - just `Skyrim Special Edition` folder inside the `.../steamapps/common/`
  + **`Mod file path`** - path of the `Plugins.txt file`, which contains info about current mod list. Used by game. Located in `AppData/Local` folder.
* **`Buttons`**:
  + **`Select loader compatdata`** - after you launch `skse64_loader.exe` at least once using Proton (check in Steam -> Rightclick an app -> Properties -> Compatibility, then set the proton version) select the folder inside `../steamapps/compatdata/`, related to the app.
  It usually comes with a huge number. The launcher ID will update after that.
  + **`ReInit Text Blocks`** - ask app to auto-detect missing folder/filepaths. Automatically called at launch.
  + **`The rest of the buttons`** - their names speak for themselves. Please initialize manually if textboxes above are null or empty.

> [!CAUTION]
> Canceling mod's instalation is not included in program yet, be careful.

> [!WARNING]
> Please, ignore the `shortcuts.vdf path` duplicate, I always forget to delete unnecessary stuff.

### Launching application
The only file you need to launch is called `VokunModManager`, that will be inside the archive's folder.
On your first launch, the program will try to initialize all the paths except the loader's `compatdata` folder. If everything will work correctly, you'll see full paths above the buttons. If not, please manually select all the necessary stuff.
Next, just install mods, enable/disable them and go play. Just make sure you did everything correct (installation, paths configuration).

> [!WARNING]
> Please, before you start just fill all the TextBoxes above the buttons. You should initialize every path (if it's not detected automatically) and make it look like this (first two strings can be ignored on Windows):
> <img alt="image" src="assets/preview2.png" />
> Once you finish with all path and file's initializations, move on.

###
You can check if you did everything correct in game's "Creations" tab:

<img height="300" alt="image" src="assets/previewGameLoadOrder.jpg" />

Example, College of Winterhold main hall and SkyHUB dot in the centre:

<img height="300" alt="image" src="assets/previewCollegeHall.jpg" />

## Features
Completed:
* Launching game through the `skse64_loader.exe`.
* Installing mods straight from archive to `Data` folder.
* Installing mods via FOMOD config.
* Enabling/disabling the mods.
* Changing mods' load order (manually).
* Cancel mod installation (only with config-included ones).
* Cross-platform support (both Windows and Linux).

Coming:
* Automatic mods sorting (priorities, etc.).
* Deleting mods.
* Preset/backup system.
* Nexus integration?
* More mods support.
* Something else...

## Possible issues

### Mods
Not all type of mods are handled by Vokun, so if you install something like FNIS, or complex mods without FOMOD and other stuff, it may give you an exception or just dump all the files to `Data` folder and make a mess.

Also, the "delete mod" feature is not ready yet, just like the profile/preset system, careful with deleting mods manually.

I'll improve this manager and make it better.. one day... if i feel so.. :)
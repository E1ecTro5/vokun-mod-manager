# Vokun Mod Manager
Vokun is a semi-automatic mod manager to help you install and organize mods for Skyrim Special Edition (Steam version) by using skse64_loader.exe as the main launcher on Linux. It can install fles from archive straight to the Data folder and instantly enable mods (NOT ALL OF THEM).
If you were looking for something like Mod Organizer 2 or Vortex Mod Manager that works on Linux, this is not the place. Instead, check out [this repository](https://github.com/SulfurNitride/NaK) by [@SulfurNitride](https://github.com/SulfurNitride).

## Installation

### Before you use it
Before installing the manager, make sure you have [Skyrim Script Extender](https://skse.silverlock.org/) installed first. Add `skse64_loader.exe` to your steam library as a non-steam game. Set the proton version (I personally used Proton  10.0-4), and in the
launch options add the next line:
``````
STEAM_COMPAT_DATA_PATH=/home/epsilon/.local/share/Steam/steamapps/compatdata/489830 %command%
``````

### Installing

Just download the latest release from the [release pages](https://github.com/E1ecTro5/vokun-mod-manager/releases). Yeah, this is a portable software, no need to install, just download and run <ins>**VokunModManager**</ins> file. Just don't touch anything else.
> [!CAUTION]
> This software was tested ONLY on Linux (CachyOS is you're interested), so I don't expect it to work on other platforms. Cross-platform patch is coming one day (or not).

## How to use

### So, here is the UI design:
<img width="1480" height="824" alt="image" src="https://github.com/user-attachments/assets/a1fbcb90-6478-454c-9a31-2ce955324a86" />

* `Current mod list` - on the left side is the list of mods (.esp/.esm/.esl) mentioned in the `Plugins.txt` file. The checkbox represents the `*` symbol in a string, saying whether the mod is currently on or off.
You're also able to reorder them by drag-and-dropping, thanks to [@aldelaro5](https://github.com/aldelaro5) for the [solution](https://github.com/AvaloniaUI/Avalonia/discussions/10877).
> [!NOTE]
> Note that if you disable some of the mods and load a save with the disabled mod included, that mod will automatically turn on

* `Directories and files` - just some info about:
  + `Launcher ID` - `skse64_loader.exe`'s steam ID to launch it properly.
  + `shortcuts.vdf` file - this file is needed to calculate the loader's ID properly.
  + `Game folder` - just `Skyrim Special Edition` folder inside the `.../steamapps/common/`
  + `Mod file path` - path of the `Plugins.txt file`, which contains info about current mod list. Used by game. Located in compatdata folder.
* Buttons:
  + `Select loader compatdata` - after you launch `skse64_loader.ex`e at least once using Proton (check in Steam -> Rightclick an app -> Properties -> Compatibility, then set the proton version) select the folder inside `../steamapps/compatdata/`, related to the app.
  It usually comes with a huge number. The launcher ID will update after that.
  + `The rest of the buttons` - their names speak for themselves. Please initialize manually if textboxes above are null or empty.

> [!CAUTION]
> Canceling mod's instalation is not included in program yet, be careful.

### Launching application
The only file you need to launch is called `VokunModManager`, no .exe or something else.
On your first launch, the program will try to initialize all the paths except the loader's `compatdata` folder. If everything will work correctly, you'll see full paths above the buttons. If not, please manually select all the necessary stuff.
Next, just install mods, enable/disable them and go play. Just make sure you did everything correct (installation, paths configuration).

> [!WARNING]
> It's most likely that I forgot to handle some of the exceptions, so before you start just fill all the TextBoxes above the buttons. You should initialize every path (if it's not detected automatically) and make it look like this:
> <img width="967" height="129" alt="image" src="https://github.com/user-attachments/assets/d660ab7c-edd2-44c5-84bf-2b3a1bc21b92" />
> Once you finish with all path and file's initializations, move on.

## Features
Completed:
* Launching game through the `skse64_loader.exe`.
* Installing mods straight from archive to `Data` folder.
* Installing mods via FOMOD config.
* Enabling/disabling the mods.
* Changing mods' load order (manually).

Coming:
* Automatic mods sorting (priorities, etc.).
* Deleting mods.
* Cancel mod installation (yes, this is not included yet, be careful).
* Progress bar with current installing file (some sort of a specific Window).
* Preset/backup system.
* Cross-platform?
* Nexus integration?
* Something else...

## Possible issues

### Mods
Not all type of mods are handled by Vokun, so if you install something like FNIS, or complex mods without FOMOD and other stuff, it may give you an exception or just dump all the files to `Data` folder and make a mess.

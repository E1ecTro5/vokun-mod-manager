# Contributing to Vokun Mod Manager

First off, thank you for considering contributing to Vokun Mod Manager!

## How Can You Help?

### Reporting Bugs
Before creating a bug report, please check existing issues to see if the bug has already been reported.

When creating a bug report, please include:
* **OS & Version:** Windows or Linux, version of OS, its kernel version, and distribution.
* **Steps to Reproduce:** Clear, step-by-step instructions (video, if you want).
* **Expected vs Actual Behavior:** What you expected to happen and what actually happened.
* **Logs:** Relevant error messages or logs if applicable (in-app logs, SKSE64 logs, etc.).

### Suggesting Features
Enhancement suggestions are tracked as GitHub Issues. 
Please create an issue detailing:
* **Use Case:** The problem this feature solves or the workflow it improves.
* **Proposed Solution:** How you envision this feature working in the app.
* **Alternatives Considered:** Any workaround solutions or alternative implementations you've thought of.

## Development Setup & Tech Stack

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/download) (or latest stable)
* Any IDE with C# support (I personally used [JetBrains Rider](https://www.jetbrains.com/rider/))

### Tech Stack
* C# v10.0
* AvaloniaUI v11.3.11 (update to v12 once its dependencies are ready).
  + Avalonia.Controls.DataGrid v11.3.11
  + Avalonia.Desktop v11.3.11
  + Avalonia.Themes.Fluent v11.3.11
  + Avalonia.Fonts.Inter v11.3.11
  + Avalonia.Diagnostics v11.3.11
  + Avalonia.Xaml.Behaviors v11.3.0.6
  + Avalonia.Xaml.Interactions v11.3.0.6
  + Avalonia.Xaml.Interactions.DragAndDrop v11.3.0.6
  + LoadingIndicators.Avalonia v11.0.11.1
  + MessageBox.Avalonia v3.3.1.1
* CommunityToolkit.Mvvm v8.2.1 (for MVVM pattern and bindings)
* SharpCompress v0.44.5 (main archive handler in app)
* SteamKit2 v3.4.0 (I used this in previous solutions, not needed for now; will be deleted)
* System.IO.Hashing v10.0.2 (I don't remember why I added this :3)

### Repository Structure
Two main projects inside the solutions are:
+ **`ToolLauncher`** (Console App): A lightweight helper tool acting as a bridge between Steam's Proton and external modding tools (FNIS, BodySlide, OutfitStudio, etc.) on Linux.
   * `How it works?` - On Linux, it temporarily swaps `SkyrimSELauncher.exe` with `ToolLauncher.exe` via symlink/backup, passes the target tool path via `vokun_tool_config.txt`,
  and triggers Steam game launch (`steam://rungameid/489830`) so Proton executes the tool within the game's prefix.
+ **`VokunModManager`** - the main project, contains everything you need.
  * `Views/MainWindow.axaml` - main window, that you see once you open the app.
  * `ViewModels/MainWindowViewModel` - view model for main window. Contains everything related to what's you see on it.
  * `Misc/FileManager` - get/select the folder/file/archive paths.
  * `Misc/FomodManager` - it's called "Fomod" but actually, this is the main mod installation file. Both with Fomod config and without it.
  * `Misc/AutoDetector` - contains method for finding files/folder like `Data` folder of the game, files like `Plugins.txt`, tools like `OutfitStudio` and etc. Searches both on Windows and Linux.
  * `Misc/ModListManager` - reads and updates `Plugins.txt`, scans `.esp`/`.bsa` files inside the `Data` directory.
  * `Misc/PathResolver` - normalizes case-sensitivity issues (`textures` vs `Textures`), crucial for Linux native filesystems.
  * `Misc/UiLoggerService` and `Misc/MsgBoxManager` - both used for logging. Probably will disappear in future, due to refactor.

## Development Workflow

### 1. Clone & Branch
Always create your feature or bugfix branch off the **`develop`** branch:
```bash
git clone https://github.com/E1ecTro5/VokunModManager.git
git checkout develop
git checkout -b feature/your-feature-name
```
Or just use GUI tools, if your IDE allows.

### 2. Local Run (Development)

To run the project locally during development:
```bash
dotnet run --project VokunModManager
```
Or just use IDE tools.

### 3. Building & Publishing Releases

The main `.csproj` is configured to automatically build and publish `ToolLauncher.exe` into `Assets/Utils/` before building the main app.

To produce a self-contained, single-file release build:

Linux:
```bash
dotnet publish VokunModManager -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true
```

Windows:
```bash
dotnet publish VokunModManager -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

I personally did it inside the IDE terminal.

### 4. Submitting Pull Requests
Target develop branch for all PRs.
Ensure all changes are tested on your local setup (mention whether you tested on Linux/Proton or Windows in your PR description, pls :3).

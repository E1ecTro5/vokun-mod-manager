using System.Diagnostics;
using System.Runtime.InteropServices;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.Utils;

public class ToolLauncher : IToolLauncher
{
    private readonly IAppConfig _appConfig;
    private readonly ILoggerService _logger;
    private string? GameFolderPath => _appConfig.GameFolderPath;

    public ToolLauncher(IAppConfig appConfig, ILoggerService logger)
    {
        _appConfig = appConfig;
        _logger = logger;
    }

    public async Task LaunchInternalToolAsync(string relativeToolPath, Action? preLaunchSetup = null)
    {
        if (string.IsNullOrEmpty(GameFolderPath))
        {
            _logger.Log("Game folder path is not configured.", LogLevel.Error);
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            await LaunchViaProtonLinuxAsync(GameFolderPath, relativeToolPath, preLaunchSetup);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            LaunchNativeWindows(GameFolderPath, relativeToolPath);
        }
    }

    public Task LaunchExternalToolAsync(string executablePath, string? arguments = null)
    {
        if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
        {
            _logger.Log($"External tool executable standard path invalid: {executablePath}", LogLevel.Warning);
            return Task.CompletedTask;
        }

        try
        {
            var workingDir = Path.GetDirectoryName(executablePath);
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDir ?? string.Empty,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _logger.Log($"Failed to launch external tool '{executablePath}': {ex.Message}", LogLevel.Error);
        }

        return Task.CompletedTask;
    }

    private async Task LaunchViaProtonLinuxAsync(string gameFolderPath, string relativeToolPath, Action? preLaunchSetup)
    {
        string launcherPath = Path.Combine(gameFolderPath, "SkyrimSELauncher.exe");
        string backupLauncherPath = Path.Combine(gameFolderPath, "SkyrimSELauncher_backup.exe");
        string helperSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Utils", "ToolLauncher.exe");
        string configPath = Path.Combine(gameFolderPath, "vokun_tool_config.txt");

        try
        {
            if (!File.Exists(helperSource))
            {
                _logger.Log($"Tool launcher helper not found at: {helperSource}", LogLevel.Error);
                return;
            }

            // backup orig launcher
            if (File.Exists(launcherPath) && !File.Exists(backupLauncherPath))
            {
                var fileInfo = new FileInfo(launcherPath);
                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
                {
                    File.Move(launcherPath, backupLauncherPath);
                }
            }

            // for specific reasons (like fix error 2001 in FNIS)
            preLaunchSetup?.Invoke();

            // write path to our tool, so it'll launch the right one
            await File.WriteAllTextAsync(configPath, relativeToolPath);

            // replace the launcher
            if (File.Exists(launcherPath)) File.Delete(launcherPath);
            File.Copy(helperSource, launcherPath, overwrite: true);

            // run
            Process.Start(new ProcessStartInfo
            {
                FileName = "steam://rungameid/489830",
                UseShellExecute = true
            });

            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            _logger.Log($"Error launching tool in Proton: {ex.Message}", LogLevel.Error);
        }
    }

    private void LaunchNativeWindows(string gameFolderPath, string relativeToolPath)
    {
        try
        {
            string absolutePath = Path.Combine(gameFolderPath, relativeToolPath);
            
            if (!File.Exists(absolutePath))
            {
                _logger.Log($"Internal tool executable not found on Windows: {absolutePath}", LogLevel.Error);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = absolutePath,
                WorkingDirectory = gameFolderPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _logger.Log($"Error launching internal tool on Windows: {ex.Message}", LogLevel.Error);
        }
    }
}
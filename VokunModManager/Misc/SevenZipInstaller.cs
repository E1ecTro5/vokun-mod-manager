using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VokunModManager.Misc;

public class SevenZipInstaller
{
    public SevenZipInstaller()
    {
        PrepareDirectory();
    }
    
    private string _tempFolder;
    
    public async Task MoveModFiles(string? startDirectory = null)
    {
        if(string.IsNullOrEmpty(startDirectory)) startDirectory = _tempFolder;
        string destinationDirectory = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");

        MergeMoveDirectory(startDirectory, destinationDirectory);
    }
    
    private static void MergeMoveDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Move(file, destFile, overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(directory));

            // Рекурсивно объединяем подпапки
            MergeMoveDirectory(directory, destSubDir);
        }
    }

    public async Task PrepareDirectory()
    {
        _tempFolder = AppConfig.Instance.TempFolder;
        Directory.Delete(_tempFolder, true);
        Directory.CreateDirectory(_tempFolder);
    }
    
    public async Task ExtractAll(string source)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "7z",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.StartInfo.ArgumentList.Add("x");                    // extract
        process.StartInfo.ArgumentList.Add(source);                 // from source
        process.StartInfo.ArgumentList.Add($"-o{_tempFolder}");     // to destination
        process.StartInfo.ArgumentList.Add("-y");                   // just say "yes" and ignore other stuff

        process.Start();
    }
    
    // you should add Cancellation Token later
    //await process.WaitForExitAsync();
}
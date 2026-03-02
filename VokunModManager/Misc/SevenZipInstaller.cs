using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace VokunModManager.Misc;

public class SevenZipInstaller
{
    public static readonly string DefaultDestination = AppConfig.Instance.TempFolder;
    
    public static async Task ExtractMods()
    {
        
    }

    public static async Task PrepareDirectory()
    { 
        Directory.Delete(AppConfig.Instance.TempFolder, true);
        Directory.CreateDirectory(DefaultDestination);
    }
    
    public static async Task ExtractAll(string source)
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
        process.StartInfo.ArgumentList.Add($"-o{DefaultDestination}");     // to destination
        process.StartInfo.ArgumentList.Add("-y");                   // just say "yes" and ignore other stuff

        process.Start();
    }
    
    // you should add Cancellation Token later
    //await process.WaitForExitAsync();
}
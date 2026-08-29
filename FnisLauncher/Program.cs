using System.Diagnostics;

namespace FnisLauncher;

class Program
{
    static void Main(string[] args)
    {
        // hello message
        Console.WriteLine("==========================================");
        Console.WriteLine("       Vokun Mod Manager: FNIS Helper     ");
        Console.WriteLine("==========================================");

        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine($"[DEBUG] Base Directory: {baseDir}");

            // path to Data/tools/GenerateFNIS_for_Users
            string fnisFolder = Path.Combine(baseDir, "Data", "tools", "GenerateFNIS_for_Users");
            string fnisExePath = Path.Combine(fnisFolder, "GenerateFNISForUsers.exe");

            Console.WriteLine($"[DEBUG] Target FNIS Folder: {fnisFolder}");
            Console.WriteLine($"[DEBUG] Target Executable:  {fnisExePath}");

            if (!File.Exists(fnisExePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] GenerateFNISForUsers.exe not found at path!");
                Console.ResetColor();
                PauseAndExit();
                return;
            }

            // set FNIS folder as a currnet directory
            Directory.SetCurrentDirectory(fnisFolder);
            Console.WriteLine($"[DEBUG] Current Directory set to: {Directory.GetCurrentDirectory()}");

            Console.WriteLine("[INFO] Starting GenerateFNISForUsers.exe...");

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fnisExePath,
                WorkingDirectory = fnisFolder,
                UseShellExecute = false
            };

            process.Start();
            process.WaitForExit();

            Console.WriteLine($"[INFO] FNIS exited with code: {process.ExitCode}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL ERROR] Exception occurred:");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
            PauseAndExit();
        }
    }

    private static void PauseAndExit()
    {
        Console.WriteLine("\nPress ANY key to close this window...");
        Console.ReadKey();
    }
}
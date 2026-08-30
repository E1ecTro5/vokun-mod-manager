using System.Diagnostics;

namespace ToolLauncher;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("     Vokun Mod Manager: Tool Launcher     ");
        Console.WriteLine("==========================================");

        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, "vokun_tool_config.txt"); // special config for tools

            if (!File.Exists(configPath))
            {
                PrintError($"Configuration file not found at: {configPath}");
                PauseAndExit();
                return;
            }

            string relativePath = File.ReadAllText(configPath).Trim().Replace('/', '\\');
            
            // path to target .exe file
            string targetExePath = Path.Combine(baseDir, relativePath);
            string workingDir = Path.GetDirectoryName(targetExePath)!;

            Console.WriteLine($"[DEBUG] Base Directory: {baseDir}");
            Console.WriteLine($"[DEBUG] Target Executable: {targetExePath}");
            Console.WriteLine($"[DEBUG] Working Directory: {workingDir}");

            if (!File.Exists(targetExePath))
            {
                PrintError($"Target executable not found at path: {targetExePath}");
                PauseAndExit();
                return;
            }

            Directory.SetCurrentDirectory(workingDir);
            Console.WriteLine($"[INFO] Starting process...");

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = targetExePath,
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            process.Start();
            process.WaitForExit();

            Console.WriteLine($"[INFO] Tool exited with code: {process.ExitCode}");
        }
        catch (Exception ex)
        {
            PrintError($"Exception occurred:\n{ex}");
            PauseAndExit();
        }
    }

    private static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    private static void PauseAndExit()
    {
        Console.WriteLine("\nPress ANY key to close this window...");
        Console.ReadKey();
    }
}
using System.Runtime.InteropServices;

namespace VokunModManager.Utils;

public static class PathResolver
{
    /// <summary>
    /// Returns the real path on disk, depending on existing folders' specific registers.
    /// </summary>
    public static string NormalizePathForOs(string fullPath)
    {
        // fixes 'meshes' and 'Meshes' types of problems
        // take the existing one, ignore the others
        
        // Windows doesn't need this
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || string.IsNullOrWhiteSpace(fullPath)) return fullPath;

        string root = Path.GetPathRoot(fullPath) ?? "";
        string relative = fullPath.Substring(root.Length);
        string[] segments = relative.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

        string currentPath = root;

        foreach (var segment in segments)
        {
            string candidate = Path.Combine(currentPath, segment);
            
            if (Directory.Exists(candidate) || File.Exists(candidate))
            {
                currentPath = candidate;
                continue;
            }
            
            if (Directory.Exists(currentPath))
            {
                var match = Directory.EnumerateFileSystemEntries(currentPath)
                    .FirstOrDefault(e => Path.GetFileName(e).Equals(segment, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    // found existing file? use its name.
                    currentPath = match;
                    continue;
                }
            }

            // if nothing found just create it with original registers
            currentPath = candidate;
        }

        return currentPath;
    }
}
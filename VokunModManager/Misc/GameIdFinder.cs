using System;
using System.IO;
using System.Threading.Tasks;
using SteamKit2;

namespace VokunModManager.Misc;

public static class GameIdFinder
{
    private static async Task<uint> GetShortIdFromVdf(string vdfPath, string targetExePath)
    {
        if (!File.Exists(vdfPath)) throw new FileNotFoundException("vdfPath is null or empty!", vdfPath);
        
        await using var fs = File.OpenRead(vdfPath);
        var kv = new KeyValue();

        if (!kv.TryReadAsBinary(fs))
            return 0;

        // there goes some Valve shi, I'll doc here later...
        foreach (var shortcut in kv.Children)
        {
            var exe = shortcut["exe"].Value;

            // make sure
            if (!string.IsNullOrEmpty(exe) && exe.Trim('"') == targetExePath)
            {
                //get appid (not the real one)
                if (int.TryParse(shortcut["appid"].Value, out int rawId))
                {
                    // this num will be used in GetLongId
                    return (uint)rawId;
                }
            }
        }

        return 0;
    }
    
    public static async Task<ulong> GetLongId(string exePath)
    {
        string vdfPath = AppConfig.Instance.VdfConfigPath;
        
        if(vdfPath == null) throw new NullReferenceException("vdfPath is null!");
        if(exePath == null) throw new NullReferenceException("exePath is null!");
        
        uint num = await GetShortIdFromVdf(vdfPath, exePath);

        // according to Valve rules, this is how it should be
        // 0x02000000 — "Non-Steam Game" flag
        ulong longId = ((ulong)num << 32) | 0x02000000;
    
        return longId;
    }
}
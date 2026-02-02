using System;
using System.IO;
using System.Text;
using System.IO.Hashing;

using SteamKit2;

namespace VokunModManager.Misc;

public class GameIdFinder
{
    public uint GetShortIdFromVdf(string vdfPath, string targetExePath)
    {
        if (!File.Exists(vdfPath)) return 0; // throw exep?
        
        using var fs = File.OpenRead(vdfPath);
        var kv = new KeyValue();

        if (!kv.TryReadAsBinary(fs))
            return 0;

        // there goes some Valve shi, I'll doc here later..
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
    
    public ulong GetLongId(string exePath)
    {
        string vdfPath = AppConfig.Instance.VdfConfigPath;
        uint num = GetShortIdFromVdf(vdfPath, exePath);

        // according to Valve rules, this is how it should be
        // 0x02000000 — "Non-Steam Game" flag
        ulong longId = ((ulong)num << 32) | 0x02000000;
    
        return longId;
    }
}
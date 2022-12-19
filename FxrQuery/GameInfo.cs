using SoulsFormats;

namespace FxrQuery;

public class GameInfo
{
    public enum GameType
    {
        DarkSouls3,
        EldenRing
    }

    public GameType Type { get; }
    
    public string Name { get; }
    
    public EMEVD.Game Format { get; }
    
    public int[]? FfxEvents { get; } 
    
    public string ParamDefPath { get; }

    public GameInfo(string gameDirectory)
    {
        if (gameDirectory.Contains("DARK SOULS III"))
        {
            Type = GameType.DarkSouls3;
            Name = "Dark Souls 3";
            Format = EMEVD.Game.DarkSouls3;
            FfxEvents = new [] { 95, 96, 99, 100, 101, 108, 110, 112, 114, 115, 116, 118, 119, 120, 121, 122 };
            ParamDefPath = "DS3\\Defs";
        }
        else if (gameDirectory.Contains("ELDEN RING"))
        {
            Type = GameType.EldenRing;
            Name = "Elden Ring";
            Format = EMEVD.Game.Sekiro;
            FfxEvents = new[] { 95, 96, 104, 110, 112, 114, 115, 116, 118, 119, 120, 121, 122 };
            ParamDefPath = "ER\\Defs";
            
            string cwd = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists($"{cwd}\\oo2core_6_win64.dll"))
            {
                throw new FileNotFoundException("oo2core_6_win64.dll could not be found in the executable directory.");
            }
        }
        else
        {
            throw new ArgumentException("Game directory does is not from a supported game.");
        }
    }
}
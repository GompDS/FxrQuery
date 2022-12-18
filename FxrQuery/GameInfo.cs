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
    
    public int[]? FfxEvents { get; } 
    
    public string ParamDefPath { get; }

    public GameInfo(string gameDirectory)
    {
        if (gameDirectory.Contains("DARK SOULS III"))
        {
            Type = GameType.DarkSouls3;
            Name = "Dark Souls 3";
            FfxEvents = new [] { 95, 96, 99, 100, 101, 108, 110, 112, 114, 115, 116, 118, 119, 120, 121, 122 };
            ParamDefPath = "DS3\\Defs";
        }
        else if (gameDirectory.Contains("ELDEN RING"))
        {
            Type = GameType.EldenRing;
            Name = "Elden Ring";
            FfxEvents = new[] { 95, 96, 104, 110, 112, 114, 115, 116, 118, 119, 120, 121, 122 };
            ParamDefPath = "ER\\Defs";
        }
        else
        {
            throw new ArgumentException("Game directory does is not from a supported game.");
        }
    }
}
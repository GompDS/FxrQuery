using System.Xml.Linq;
using SoulsAssetPipeline.Animation;
using SoulsFormats;

namespace FxrQuery;

public class Program
{
    public static void Main(string[] args)
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string gameDirectory = XElement.Load($"{cwd}\\Config.xml").Elements("gameDirectory").First().FirstAttribute.Value;

        if (!Directory.Exists(gameDirectory))
        {
            throw new DirectoryNotFoundException("The path for gameDirectory in Config.xml is not a directory.");
        }

        GameInfo game = new(gameDirectory);
        Console.WriteLine($"Game Directory: \"{gameDirectory}\"");
        
        if (!args[0].EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Invalid file extension. Only .csv is allowed.");
        }

        Console.WriteLine("Loaded csv. Reading FXR IDs...");
        HashSet<int> unusedFxrIds = new();
        HashSet<int> usedFxrIds = new();
        HashSet<int> extraFxrIds = new();
        foreach (string line in File.ReadAllLines(args[0]))
        {
            string[] values = line.Split(",");
            foreach (string value in values.Where(x => int.TryParse(x, out int _)))
            {
                if (int.Parse(value) > 0)
                {
                    unusedFxrIds.Add(int.Parse(value));
                }
            }
        }

        int csvFxrIdsCount = unusedFxrIds.Count;
        Console.WriteLine($"Found {csvFxrIdsCount} potential FXR IDs.");
       
        string chrDirectory = $"{gameDirectory}\\chr";
        if (Directory.Exists(chrDirectory))
        {
            Console.WriteLine("Searching character animations for FXR IDs...");
            foreach (string anibndPath in Directory.EnumerateFiles(chrDirectory, "*.anibnd.dcx"))
            {
                if (BND4.IsRead(anibndPath, out BND4 bnd))
                {
                    SearchAllTae(bnd, game, unusedFxrIds, usedFxrIds, extraFxrIds);
                }
            }
        }
        
        string objDirectory = $"{gameDirectory}\\obj";
        if (Directory.Exists(objDirectory))
        {
            Console.WriteLine("Searching object animations for FXR IDs...");
            foreach (string objbndPath in Directory.EnumerateFiles(objDirectory, "*.objbnd.dcx"))
            {
                if (BND4.IsRead(objbndPath, out BND4 bnd) && bnd.Files.Any(x => x.Name.EndsWith(".anibnd")))
                {
                    if (BND4.IsRead(bnd.Files.First(x => x.Name.EndsWith(".anibnd")).Bytes, out BND4 innerBnd))
                    {
                        SearchAllTae(innerBnd, game, unusedFxrIds, usedFxrIds, extraFxrIds);
                    }
                }
            }
        }

        string mapDirectory = $"{gameDirectory}\\map\\mapstudio";
        if (Directory.Exists(mapDirectory))
        {
            Console.WriteLine("Searching maps for FXR IDs...");
            foreach (string msbPath in Directory.EnumerateFiles(mapDirectory, "*.msb.dcx"))
            {
                if (MSB3.IsRead(msbPath, out MSB3 msb))
                {
                    SearchMsb(msb, unusedFxrIds, usedFxrIds, extraFxrIds);
                }
            }
        }

        Console.WriteLine("Searching game params for FXR IDs...");
        
        List<PARAMDEF> paramDefs = new();
        foreach (string path in Directory.GetFiles($"{cwd}\\Paramdex\\{game.ParamDefPath}", "*.xml"))
        {
            PARAMDEF paramDef = PARAMDEF.XmlDeserialize(path);
            paramDefs.Add(paramDef);
        }
        
        Dictionary<string, PARAM> parms = new();
        BND4 paramBnd = new();
        if (game.Type == GameInfo.GameType.DarkSouls3)
        {
            paramBnd = SFUtil.DecryptDS3Regulation($"{gameDirectory}\\Data0.bdt");
        }
        else if (game.Type == GameInfo.GameType.EldenRing)
        {
            
            paramBnd = SFUtil.DecryptERRegulation($"{gameDirectory}\\regulation.bin");
        }
        foreach (BinderFile file in paramBnd.Files)
        {
            if (!file.Name.ToLower().EndsWith(".param"))
            {
                continue;
            }
            string name = Path.GetFileNameWithoutExtension(file.Name);
            PARAM param = PARAM.Read(file.Bytes);
            if (param.ApplyParamdefCarefully(paramDefs))
            {
                parms[name] = param;
            }
        }
        foreach (PARAM.Cell cell in parms.Values.Where(x => !x.ParamType.Equals("SP_EFFECT_PARAM_ST")).SelectMany(x =>
                     x.Rows.SelectMany(y => y.Cells.Where(z => 
                         z.Def.DisplayName.ToLower().Contains("fx") && z.Def.DisplayType == PARAMDEF.DefType.s32))))
        { 
            UpdateFxrSets((int) cell.Value, unusedFxrIds, usedFxrIds, extraFxrIds);
        }

        WriteFxrIds(unusedFxrIds, $"UnusedFxrIds_{Path.GetFileNameWithoutExtension(args[0])}.txt", game);
        WriteFxrIds(usedFxrIds, $"UsedFxrIds_{Path.GetFileNameWithoutExtension(args[0])}.txt", game);
        WriteFxrIds(extraFxrIds, $"ExtraFxrIds_{Path.GetFileNameWithoutExtension(args[0])}.txt", game);

        Console.WriteLine($"\nFinal results for {game.Name} using \"{Path.GetFileName(args[0])}\":");
        Console.WriteLine($"Total FXR count from CSV: {csvFxrIdsCount}");
        Console.WriteLine($"Unused FXR count: {unusedFxrIds.Count}");
        Console.WriteLine($"Used FXR count: {usedFxrIds.Count} (Includes extra FXR count)");
        Console.WriteLine($"Extra FXR count: {extraFxrIds.Count} (FXR that were used, but not in the CSV)");
        Console.WriteLine($"\nPress any key to quit...");
        Console.ReadKey();
    }

    private static void UpdateFxrSets(int ffxId, HashSet<int> unusedFxrIds, HashSet<int> usedFxrIds,
        HashSet<int> extraFxrIds)
    {
        if (ffxId > 0)
        {
            if (unusedFxrIds.Contains(ffxId))
            {
                unusedFxrIds.Remove(ffxId);
                usedFxrIds.Add(ffxId);
            }
            else if (!usedFxrIds.Contains(ffxId))
            {
                extraFxrIds.Add(ffxId);
                usedFxrIds.Add(ffxId);
            }
        }
    }

    private static void SearchAllTae(BND4 bnd, GameInfo game, HashSet<int> unusedFxrIds, HashSet<int> usedFxrIds, HashSet<int> extraFxrIds)
    {
        foreach (TAE tae in bnd.Files.Where(x => TAE.Is(x.Bytes)).Select(x => TAE.Read(x.Bytes)))
        {
            foreach (TAE.Event ev in tae.Animations.SelectMany(x => 
                         x.Events.Where(y => game.FfxEvents.Contains(y.Type))))
            {
                if (ev.Type == 120)
                {
                    for (int i = 0; i < 12; i += 4)
                    {
                        int ffxId = ev.ReadParameterInt32(tae.BigEndian, i);
                        UpdateFxrSets(ffxId, unusedFxrIds, usedFxrIds, extraFxrIds);
                    }
                }
                else
                {
                    int ffxId = ev.ReadParameterInt32(tae.BigEndian, 0);
                    UpdateFxrSets(ffxId, unusedFxrIds, usedFxrIds, extraFxrIds);
                }
            }
        }
    }

    private static void SearchMsb(IMsb msb, HashSet<int> unusedFxrIds, HashSet<int> usedFxrIds,
        HashSet<int> extraFxrIds)
    {
        if (MSB3.Is(msb.Write()))
        {
            msb = MSB3.Read(msb.Write());
            foreach (MSB3.Region.SFX sfx in ((MSB3) msb).Regions.SFX)
            {
                UpdateFxrSets(sfx.EffectID, unusedFxrIds, usedFxrIds, extraFxrIds);
            }

            foreach (MSB3.Region.WindSFX windSfx in ((MSB3) msb).Regions.WindSFX)
            {
                UpdateFxrSets(windSfx.EffectID, unusedFxrIds, usedFxrIds, extraFxrIds);
            }
        }
        else if (MSBE.Is(msb.Write()))
        {
            msb = MSBE.Read(msb.Write());
            foreach (MSBE.Region.SFX sfx in ((MSBE) msb).Regions.SFX)
            {
                UpdateFxrSets(sfx.EffectID, unusedFxrIds, usedFxrIds, extraFxrIds);
            }

            foreach (MSBE.Region.WindSFX windSfx in ((MSBE) msb).Regions.WindSFX)
            {
                UpdateFxrSets(windSfx.EffectID, unusedFxrIds, usedFxrIds, extraFxrIds);
            }
        }
    }

    private static void WriteFxrIds(HashSet<int> fxrIds, string fileName, GameInfo game)
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        StreamWriter sw = new($"{cwd}\\{fileName}");
        sw.WriteLine($"Game Searched: {game.Name}");
        sw.WriteLine($"FXR Count: {fxrIds.Count}");
        sw.WriteLine("--------------------");
        foreach (int fxrId in fxrIds.OrderBy(x => x))
        {
            sw.WriteLine($"{fxrId}");
        }
        sw.Close();
    }
}
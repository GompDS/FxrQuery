using SoulsFormats;

namespace FxrQuery.Util;

public static class EMEVDUtil
{
    public static int ReadArgInt32(this EMEVD.Instruction ins, long index)
    {
        if (ins.ArgData.Length > index + 3)
        {
            byte[] argBytes = ins.ArgData;
            byte[] intBytes = new byte[4];
            Array.Copy(argBytes, index, intBytes, 0, 4);
            /*
            if (isBigEndian)
            {
                Array.Reverse(intBytes);
            }*/

            return BitConverter.ToInt32(intBytes, 0);
        }

        return -1;
    }

    public static HashSet<int> GetNestedSfxInstructionFxrIds(EMEVD initializerEmevd, EMEVD eventEmevd, int bank, int id, int eventIdStartIndex, long paramStartIndex)
    {
        HashSet<int> fxrIds = new();
        
        foreach (EMEVD.Instruction initializer in initializerEmevd.Events.SelectMany(x => 
                     x.Instructions.Where(y => y.ID == id && y.Bank == bank)))
        {
            EMEVD.Event? ev = eventEmevd.Events.FirstOrDefault(x => x.ID == initializer.ReadArgInt32(eventIdStartIndex));
            if (ev == null)
            {
                continue;
            }
            foreach (EMEVD.Instruction ins in ev.Instructions.Where(x => x.Bank == 2006 && x.ID is 3 or 4 or 6))
            {
                List<EMEVD.Parameter> instructionParams =
                    ev.Parameters.Where(x => x.InstructionIndex == ev.Instructions.IndexOf(ins)).ToList();
                if (!instructionParams.Any())
                {
                    switch (ins.ID)
                    {
                        case 3:
                            fxrIds.Add(ins.ReadArgInt32(12));
                            break;
                        case 4:
                            fxrIds.Add(ins.ReadArgInt32(8));
                            break;
                        default:
                            fxrIds.Add(ins.ReadArgInt32(0));
                            break;
                    }
                    continue;
                }
                foreach (EMEVD.Parameter param in instructionParams)
                {
                    switch (ins.ID)
                    {
                        case 3 when param.TargetStartByte == 12:
                        case 4 when param.TargetStartByte == 8:
                        case 6 when param.TargetStartByte == 0:
                            fxrIds.Add(initializer.ReadArgInt32(paramStartIndex + param.SourceStartByte));
                            break;
                    }
                }
            }
        }

        return fxrIds;
    }

    public static HashSet<int> GetLooseSfxInstructionFxrIds(EMEVD emevd, int eventId)
    {
        HashSet<int> fxrIds = new();
        EMEVD.Event? ev = emevd.Events.FirstOrDefault(x => x.ID == eventId);
        if (ev != null)
        {
            foreach (EMEVD.Instruction ins in ev.Instructions.Where(x => x.Bank == 2006 && x.ID is 3 or 4 or 6))
            {
                switch (ins.ID)
                {
                    case 3:
                        fxrIds.Add(ins.ReadArgInt32(12));
                        break;
                    case 4:
                        fxrIds.Add(ins.ReadArgInt32(8));
                        break;
                    default:
                        fxrIds.Add(ins.ReadArgInt32(0));
                        break;
                }
            }
        }

        return fxrIds;
    }
}
using SoulsAssetPipeline.Animation;

namespace FxrQuery;

public static class EventExtensions
{
    public static int ReadParameterInt32(this TAE.Event ev, bool isBigEndian, int index)
    {
        byte[] paramBytes = ev.GetParameterBytes(isBigEndian);
        byte[] intBytes = new byte[4];
        Array.Copy(paramBytes, index, intBytes, 0, 4);
        if (isBigEndian)
        {
            Array.Reverse(intBytes);
        }

        return BitConverter.ToInt32(intBytes, 0);
    }
}
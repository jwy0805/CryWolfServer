using System.Numerics;

namespace Server.Util;

public class Util
{
    public static Vector3 NearestCell(Vector3 worldPosition)
    {
        float tolerance = 0.01f;
        int intX = (int)Math.Round(worldPosition.X * 4 + tolerance);
        int intZ = (int)Math.Round(worldPosition.Z * 4 + tolerance);

        return new Vector3((float)(intX * 0.25), worldPosition.Y, (float)(intZ * 0.25));
    }
}
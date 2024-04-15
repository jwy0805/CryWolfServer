using System.Numerics;
// using SharedDB;

namespace Server.Util;

public static class Extension
{
    public static float SqrMagnitude(this Vector3 vector3, Vector3 v)
    {
        return v.X * v.X + v.Y * v.Y + v.Z * v.Z;
    }
    
    // public static bool SaveChangesExtended(this SharedDbContext dbContext)
    // {
    //     try
    //     {
    //         dbContext.SaveChanges();
    //         return true;
    //     }
    //     catch (Exception e)
    //     {
    //         return false;
    //     }
    // }
}
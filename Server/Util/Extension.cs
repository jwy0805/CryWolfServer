using System.Numerics;
// using SharedDB;

namespace Server.Util;

public static class Extension
{
    public static float SqrMagnitude(this Vector3 vector3, Vector3 v)
    {
        return v.X * v.X + v.Y * v.Y + v.Z * v.Z;
    }
    
    public static Vector3 RotateAroundPoint(this Vector3 point, Vector3 pivot, float degrees)
    {
        double radians = degrees * Math.PI / 180;
        double cosTheta = Math.Cos(radians);
        double sinTheta = Math.Sin(radians);
        double dx = point.X - pivot.X;
        double dz = point.Z - pivot.Z;
        
        return new Vector3(
            (float)(cosTheta * dx - sinTheta * dz + pivot.X), 
            point.Y, 
            (float)(sinTheta * dx + cosTheta * dz + pivot.Z));
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
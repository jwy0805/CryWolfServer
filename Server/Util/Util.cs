using System.Numerics;
using Google.Protobuf.Protocol;
using MathNet.Numerics;

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

    public static double DistanceXZ(double x1, double z1, double x2, double z2)
    {
        double dx = x2 - x1;
        double dz = z2 - z1;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    public static double ScaleValueByLog(double value, double inputMin, double inputMax, double resultMin, double resultMax)
    {
        double logMin = Math.Log(inputMin);
        double logMax = Math.Log(inputMax);
        double normalized = (Math.Log(value) - logMin) / (logMax - logMin);
        double scaled = resultMin + normalized * (resultMax - resultMin);
        
        return Math.Min(Math.Max(scaled, resultMin), resultMax);
    }
    
    public static double GetRandomValueByGaussian(Random random, double min, double max, double mean, double stdDev)
    {
        double value;
        do
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            value = mean + stdDev * randStdNormal;
        } while (value < min || value > max);
        
        return value;
    }

    public static double GetRandomValueByPert(Random rnd, double min, double max, double mean, double eps = 1, double lambda = 4)
    {
        double minExt = min - eps;
        double alpha = 1 + lambda * (mean - minExt) / (max - minExt);
        double beta = 1 + lambda * (max - mean) / (max - minExt);

        double xGamma = SampleGamma(rnd, alpha, 1.0);
        double yGamma = SampleGamma(rnd, beta, 1.0);
        double y = xGamma / (xGamma + yGamma);
        double value = min + y * (max - min);
        
        return Math.Min(Math.Max(value, min), max);
    }
    
    // Marsaglia & Tsang method
    private static double SampleGamma(Random rnd, double shape, double scale)
    {
        if (shape < 1.0)
        {
            double u = rnd.NextDouble();
            return SampleGamma(rnd, shape + 1.0, scale) * Math.Pow(u, 1.0 / shape);
        }

        double d = shape - 1.0 / 3.0;
        double c = 1.0 / Math.Sqrt(9.0 * d);

        while (true)
        {
            double u1 = rnd.NextDouble();
            double u2 = rnd.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            double v = 1.0 + c * z;
            if (v <= 0) continue;

            v = v * v * v;
            double u = rnd.NextDouble();

            if (u < 1.0 - 0.0331 * Math.Pow(z, 4)) return scale * d * v;
            if (Math.Log(u) < 0.5 * Math.Pow(z, 2) + d * (1.0 - v + Math.Log(v))) return scale * d * v;
        }
    } 
    
    public static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;

    public static UnitId[] GetAllSubUnitIds(UnitId id)
    {
        var level = (int)id % 100 % 3;
        return level switch
        {
            0 => new[] { id, id - 1, id - 2 },
            1 => new[] { id },
            2 => new[] { id, id - 1 },
            _ => Array.Empty<UnitId>()
        };
    }
}
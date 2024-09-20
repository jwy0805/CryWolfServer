using Google.Protobuf.Protocol;
using Server.Game.Enchants;

namespace Server.Game;

public sealed class EnchantManager
{
    public static EnchantManager Instance { get; } = new();

    public readonly Dictionary<EnchantId, IEnchantFactory> EnchantDict = new();
    
    public interface IEnchantFactory
    {
        IEnchant CreateEnchant();
    }
    
    private class WindRoadFactory : IEnchantFactory { public IEnchant CreateEnchant() => new WindRoad(); }
    private class FireRoadFactory : IEnchantFactory { public IEnchant CreateEnchant() => new FireRoad(); }
}
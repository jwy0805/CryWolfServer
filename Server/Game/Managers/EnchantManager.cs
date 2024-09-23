using Google.Protobuf.Protocol;
using Server.Game.Enchants;

namespace Server.Game;

public sealed class EnchantManager
{
    public static EnchantManager Instance { get; } = new();
    
    private readonly Dictionary<EnchantId, IEnchantFactory> _enchantDict = new()
    {
        { EnchantId.WindRoad, new WindRoadFactory() },
        { EnchantId.FireRoad, new FireRoadFactory() },
        { EnchantId.EarthRoad, new EarthRoadFactory() }
    };

    private interface IEnchantFactory
    {
        Enchant CreateEnchant();
    }
    
    private class WindRoadFactory : IEnchantFactory { public Enchant CreateEnchant() => new WindRoad(); }
    private class FireRoadFactory : IEnchantFactory { public Enchant CreateEnchant() => new FireRoad(); }
    private class EarthRoadFactory : IEnchantFactory { public Enchant CreateEnchant() => new EarthRoad(); }
    
    public Enchant CreateEnchant(EnchantId enchantId)
    {
        var factory = _enchantDict[enchantId];
        return factory.CreateEnchant();
    }
}
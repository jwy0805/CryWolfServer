using Google.Protobuf.Protocol;

namespace Server.Game.Enchants;

public class EarthRoad : IEnchant
{
    public EnchantId EnchantId => EnchantId.EarthRoad;
    public int EnchantLevel { get; set; }
    
    public T GetModifier<T>(Enum id) where T : struct
    {
        throw new NotImplementedException();
    }
}
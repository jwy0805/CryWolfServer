using Google.Protobuf.Protocol;

namespace Server.Game.Enchants;

public class FireRoad : IEnchant
{
    public EnchantId EnchantId => EnchantId.FireRoad;
    public int EnchantLevel { get; set; }
    
    public T GetModifier<T>(Enum id) where T : struct
    {
        throw new NotImplementedException();
    }
}
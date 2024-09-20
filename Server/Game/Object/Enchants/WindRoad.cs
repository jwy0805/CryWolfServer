using Google.Protobuf.Protocol;
namespace Server.Game.Enchants;

public class WindRoad : IEnchant
{
    public EnchantId EnchantId => EnchantId.WindRoad;
    public int EnchantLevel { get; set; }
    
    public T GetModifier<T>(Enum id) where T : struct
    {
        throw new NotImplementedException();
    }
}
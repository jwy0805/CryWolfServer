using Google.Protobuf.Protocol;

namespace Server.Game.Enchants;

public interface IEnchant
{
    EnchantId EnchantId { get; }
    int EnchantLevel { get; set; }
    T GetModifier<T>(Enum id) where T : struct;
}

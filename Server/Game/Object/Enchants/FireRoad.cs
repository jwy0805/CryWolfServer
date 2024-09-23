using Google.Protobuf.Protocol;

namespace Server.Game.Enchants;

public class FireRoad : Enchant
{
    public override EnchantId EnchantId => EnchantId.FireRoad;
    public override int EnchantLevel { get; set; }
    
    public override float GetModifier(Player player, StatType statType, float baseValue)
    {
        if (player.Camp == Camp.Sheep) return baseValue;
        
        if (statType == StatType.Attack)
        {
            return EnchantLevel switch
            {
                1 => baseValue * 1.05f,
                2 => baseValue * 1.07f,
                3 => baseValue * 1.09f,
                4 => baseValue * 1.11f,
                5 => baseValue * 1.15f,
                _ => baseValue
            };
        }

        return baseValue;
    }
}
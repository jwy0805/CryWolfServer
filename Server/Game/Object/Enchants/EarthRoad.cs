using Google.Protobuf.Protocol;

namespace Server.Game.Enchants;

public class EarthRoad : Enchant
{
    public override EnchantId EnchantId => EnchantId.EarthRoad;
    public override int EnchantLevel { get; set; }
    
    public override float GetModifier(Player player, StatType statType, float baseValue)
    {
        if (player.Camp == Camp.Sheep) return baseValue;

        if (statType == StatType.Defence)
        {
            return EnchantLevel switch
            {
                1 => baseValue + 1,
                2 => baseValue * 3,
                3 => baseValue * 7,
                4 => baseValue * 9,
                5 => baseValue * 10,
                _ => baseValue
            };
        }
        
        return baseValue;
    }
}
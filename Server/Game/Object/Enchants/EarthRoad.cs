using Google.Protobuf.Protocol;

namespace Server.Game.Enchants;

public class EarthRoad : Enchant
{
    public override EnchantId EnchantId => EnchantId.EarthRoad;
    public override int EnchantLevel { get; set; }
    
    public override float GetModifier(Player player, StatType statType, float baseValue)
    {
        if (player.Faction == Faction.Sheep) return baseValue;

        if (statType == StatType.Defence)
        {
            return EnchantLevel switch
            {
                1 => baseValue + 1,
                2 => baseValue + 2,
                3 => baseValue + 3,
                4 => baseValue + 5,
                5 => baseValue + 7,
                _ => baseValue
            };
        }
        
        return baseValue;
    }
}
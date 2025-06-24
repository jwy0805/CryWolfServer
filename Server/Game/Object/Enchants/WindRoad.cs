using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game.Enchants;

public class WindRoad : Enchant
{
    public override EnchantId EnchantId => EnchantId.WindRoad;
    public override int EnchantLevel { get; set; }

    protected override void ShowEffect()
    {
        if (Room == null || EnchantLevel == 0) return;
        
        if (Room.Stopwatch.ElapsedMilliseconds > Time + EffectTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            
            var parent = Room.FindPlayer(player => player is Player { Faction: Faction.Wolf });
            var effectPos = new PositionInfo { PosX = 0, PosY = 6.1f, PosZ = 15 };
            var effectName = $"WindRoadEffect{EnchantLevel}".ToEnum<EffectId>();
            Room.SpawnEffect(effectName, parent, parent, effectPos, false, 5000);
        }   
    }
    
    public override float GetModifier(Player? player, StatType statType, float baseValue)
    {
        if (player?.Faction == Faction.Sheep || EnchantLevel == 0) return baseValue;

        if (statType == StatType.MoveSpeed)
        {
            return EnchantLevel switch
            {
                1 => baseValue * 1.05f,
                2 => baseValue * 1.07f,
                3 => baseValue * 1.09f,
                4 => baseValue * 1.12f,
                5 => baseValue * 1.15f,
                _ => baseValue
            };    
        }

        return baseValue;
        // return statType switch
        // {
        //     // 1 -> 1.01, 2 -> 1.03, 3 -> 1.06, 4 -> 1.1, 5 -> 1.15
        //     StatType.AttackSpeed => baseValue * (1 + 0.01f * EnchantLevel * ((EnchantLevel - 1) * 0.5f + 1)),
        //     _ => baseValue
        // };
    }
}
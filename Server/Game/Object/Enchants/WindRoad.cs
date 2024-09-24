using Google.Protobuf.Protocol;
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
            var effectName = (EffectId)Enum.Parse(typeof(EffectId), $"WindRoadEffect{EnchantLevel}");
            Room.SpawnEffect(effectName, parent, effectPos, false, 5000);
        }   
    }
    
    public override float GetModifier(Player? player, StatType statType, float baseValue)
    {
        if (player?.Faction == Faction.Sheep || EnchantLevel == 0) return baseValue;
        
        return statType switch
        {
            StatType.AttackSpeed => baseValue * (1 + 0.01f * EnchantLevel * ((EnchantLevel - 1) * 0.5f + 1)),
            _ => baseValue
        };
    }
}
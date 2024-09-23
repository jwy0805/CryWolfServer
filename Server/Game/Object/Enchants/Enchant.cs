using Google.Protobuf.Protocol;

namespace Server.Game.Enchants;

public class Enchant
{
    protected IJob? Job;
    protected long Time;
    protected readonly long EffectTime = 5000;
    protected readonly int CallCycle = 200;
    
    public virtual EnchantId EnchantId => EnchantId.None;
    public virtual int EnchantLevel { get; set; }
    public GameRoom? Room { get; set; }
    
    public void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        ShowEffect();   
    }
    
    protected virtual void ShowEffect() { }
    
    public virtual float GetModifier(Player player, StatType statType, float baseValue)
    {
        return baseValue;
    }
}
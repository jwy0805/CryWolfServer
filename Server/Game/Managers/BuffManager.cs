using System.Diagnostics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public sealed partial class BuffManager
{
    private const int CallCycle = 200;
    private IJob _job;
    private readonly Dictionary<BuffId, IBuffFactory> _buffDict = new() 
    {
        { BuffId.AttackBuff, new AttackBuffFactory() },
        { BuffId.AttackSpeedBuff, new AttackSpeedBuffFactory() },
        { BuffId.HealBuff, new HealBuffFactory() },
        { BuffId.HealthBuff, new HealthBuffFactory() },
        { BuffId.DefenceBuff, new DefenceBuffFactory() },
        { BuffId.MoveSpeedBuff, new MoveSpeedBuffFactory() },
        { BuffId.Invincible, new InvincibleFactory() },
        { BuffId.AttackDebuff, new AttackDebuffFactory() },
        { BuffId.AttackSpeedDebuff, new AttackSpeedDebuffFactory() },
        { BuffId.DefenceDebuff, new DefenceDebuffFactory() },
        { BuffId.MoveSpeedDebuff, new MoveSpeedDebuffFactory() },
        { BuffId.Curse, new CurseFactory() },
        { BuffId.Addicted, new AddictedFactory() },
        { BuffId.Aggro, new AggroFactory() },
        { BuffId.Burn, new BurnFactory() },
        { BuffId.Fainted, new FaintedFactory() }
    };
    
    public static BuffManager Instance { get; } = new();
    public Stopwatch Stopwatch { get; }
    public GameRoom? Room { get; set; }
    public HashSet<Buff> Buffs { get; } = new();
    
    private BuffManager()
    {
        Stopwatch = new Stopwatch();
        Stopwatch.Start();
    }
    
    public void AddBuff(BuffId buffId, BuffParamType paramType,
        GameObject master, Creature caster, float param, long duration = 10000, bool nested = false)
    {
        if (!_buffDict.TryGetValue(buffId, out var factory)) return;
        
        Buff buff = factory.CreateBuff();
        buff.Init(paramType, master, caster, param, duration, nested);

        if (master.Invincible && buff.Type == BuffType.Debuff) return;
        if (buff.Nested == false && master.Buffs.Contains(buff.Id))
        {
            Buff? b = Buffs.FirstOrDefault(b => b.Caster == caster && b.Id == buffId);
            b?.RenewBuff(duration);
        }
        else
        {
            master.AddBuff(buff);
        }
    }
    
    public void RemoveBuff(BuffId buffId, Creature master)
    {
        
    }

    public void RemoveAllBuff(Creature master)
    {
        List<Buff> removeBuff = (from buff in Buffs 
            where buff.Master.Id == master.Id select buff).ToList();
        master.Buffs.Clear();
        
        if (removeBuff.Count != 0)
        {
            foreach (var buff in removeBuff)
            {
                buff.RemoveBuff();
                Buffs.Remove(buff);
            }
        }
    }

    public void RemoveAllDebuff(Creature master)
    {
        List<Buff> removeDebuff = (from buff in Buffs 
            where buff.Master.Id == master.Id && buff.Type == BuffType.Debuff select buff).ToList();

        if (removeDebuff.Count != 0)
        {
            foreach (var debuff in removeDebuff)
            {
                debuff.RemoveBuff();
                master.Buffs.Remove(debuff.Id);
                Buffs.Remove(debuff);
            }
        }
    }

    public void Update()
    {
        if (Room == null) return;
        _job = Room.PushAfter(CallCycle, Update);
        if (Buffs.Count == 0) return;
        
        List<Buff> expiredBuff = Buffs.Where(buff => buff.UpdateBuff(Stopwatch.ElapsedMilliseconds)).ToList();
        if (expiredBuff.Count != 0)
        {
            foreach (var buff in expiredBuff)
            {
                buff.RemoveBuff();
                Buffs.Remove(buff);
            }
        }
    }
}


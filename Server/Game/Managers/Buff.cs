using Google.Protobuf.Protocol;

namespace Server.Game;

#region Enums

public enum BuffType
{
    None,
    Buff,
    Debuff,
}

public enum BuffParamType
{
    None,
    Constant,
    Percentage
}

public enum BuffId
{
    None,
    AttackBuff,
    AttackSpeedBuff,
    HealBuff,
    HealthBuff,
    DefenceBuff,
    MoveSpeedBuff,
    Invincible,
    AttackDebuff,
    AttackSpeedDebuff,
    DefenceDebuff,
    MoveSpeedDebuff,
    Curse,
    Addicted,
    Aggro,
    Burn,
    Fainted,
    AccuracyBuff,
    AccuracyDebuff,
}

#endregion

public abstract class Buff
{
    protected long StartTime;
    protected long EndTime;
    protected float Factor;
    
    public BuffId Id { get; protected set; }
    public BuffType Type { get; protected set; }
    public BuffParamType ParamType { get; private set; }
    public GameObject Master { get; private set; } = new();
    public Creature Caster { get; private set; } = new();
    public float Param { get; protected set; }
    public long Duration { get; private set; }
    public bool Nested { get; private set; }

    public virtual void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        ParamType = paramType;
        Master = master;
        Caster = caster;
        Param = param;
        Duration = duration;
        Nested = nested;
        StartTime = BuffManager.Instance.Stopwatch.ElapsedMilliseconds;
        EndTime = StartTime + Duration;
    }
        
    public virtual void CalculateFactor() { }
        
    public virtual void TriggerBuff() { }

    public virtual bool UpdateBuff(long deltaTime)
    {
        return EndTime <= deltaTime;
    }

    public virtual void RenewBuff(long duration)
    {
        EndTime = BuffManager.Instance.Stopwatch.ElapsedMilliseconds + duration;
    }
        
    public virtual void RemoveBuff()
    {
        Master.Buffs.Remove(Id);
        var buff = BuffManager.Instance.Buffs.FirstOrDefault(b => b.Master == Master && b.Id == Id);
        if (buff != null) BuffManager.Instance.Buffs.Remove(buff);
    }
}

public class AttackBuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.AttackBuff;
        Type = BuffType.Buff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.Attack * Param;
        
        if (Master.Burn) Factor *= Master.TotalFireResist / (float)100;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.AttackParam += (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.AttackParam -= (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.AttackParam -= (int)Factor;
    }
}

public class AttackSpeedBuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.AttackSpeedBuff;
        Type = BuffType.Buff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.AttackSpeed * Param;
        
        if (Master.Burn) Factor *= Master.TotalFireResist / (float)100;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.AttackSpeedParam += (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.AttackSpeedParam -= (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.AttackSpeedParam -= (int)Factor;
    }
}

public class HealBuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.HealBuff;
        Type = BuffType.Buff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.Hp * Param;
        
        if (Master.Burn) Factor *= Master.TotalFireResist / (float)100;
    }
    
    public override void TriggerBuff()
    {
        BuffManager.Instance.Room?.SpawnEffect(EffectId.StateHeal, Master, Master.PosInfo, true);
        CalculateFactor();
        Master.Hp += (int)Factor;
        RemoveBuff();
    }
}

public class HealthBuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.HealthBuff;
        Type = BuffType.Buff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.Hp * Param;
        
        if (Master.Burn) Factor *= Master.TotalFireResist / (float)100;
    }
    
    public override void TriggerBuff()
    {
        BuffManager.Instance.Room?.SpawnEffect(EffectId.StateHeal, Master, Master.PosInfo, true);
        CalculateFactor();
        Master.MaxHp += (int)Factor;
        Master.Hp += (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.MaxHp -= (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.MaxHp -= (int)Factor;
        if (Master.Hp > Master.MaxHp) Master.Hp = Master.MaxHp;
    }
}

public class DefenceBuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.DefenceBuff;
        Type = BuffType.Buff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.Defence * Param;
        
        if (Master.Burn) Factor *= Master.TotalFireResist / (float)100;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.DefenceParam += (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.DefenceParam -= (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.DefenceParam -= (int)Factor;
    }
}

public class AccuracyBuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.AccuracyBuff;
        Type = BuffType.Buff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.Accuracy * Param;
        
        if (Master.Burn) Factor *= Master.TotalFireResist / (float)100;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.AccuracyParam += (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.AccuracyParam -= (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.AccuracyParam -= (int)Factor;
    }
}

public class MoveSpeedBuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.MoveSpeedBuff;
        Type = BuffType.Buff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.MoveSpeed * Param;
        
        if (Master.Burn) Factor *= Master.TotalFireResist / (float)100;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.MoveSpeedParam += (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.MoveSpeedParam -= (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.MoveSpeedParam -= (int)Factor;
    }
}

public class Invincible : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.Invincible;
        Type = BuffType.Buff;
    }
    
    public override void TriggerBuff()
    {
        BuffManager.Instance.Room?.SpawnEffect(EffectId.HolyAura, Master, Master.PosInfo, true, (int)Duration);
        Master.Invincible = true;
    }
 
    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.Invincible = false;
    }
}

public class AttackDebuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.AttackDebuff;
        Type = BuffType.Debuff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.Attack * Param;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.AttackParam -= (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.AttackParam += (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.AttackParam += (int)Factor;
    }
}

public class AttackSpeedDebuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.AttackSpeedDebuff;
        Type = BuffType.Debuff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.AttackSpeed * Param;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.AttackSpeedParam -= (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.AttackSpeedParam += (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.AttackSpeedParam += (int)Factor;
    }
}

public class DefenceDebuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.DefenceDebuff;
        Type = BuffType.Debuff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.Defence * Param;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.DefenceParam -= (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.DefenceParam += (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.DefenceParam += (int)Factor;
    }
}

public class AccuracyDebuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.AccuracyDebuff;
        Type = BuffType.Debuff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.Accuracy * Param;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.AccuracyParam -= (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.AccuracyParam += (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.AccuracyParam += (int)Factor;
    }
}

public class MoveSpeedDebuff : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.MoveSpeedDebuff;
        Type = BuffType.Debuff;
    }
    
    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else if (ParamType == BuffParamType.Percentage) Factor = Master.MoveSpeed * Param;
    }
    
    public override void TriggerBuff()
    {
        CalculateFactor();
        Master.MoveSpeedParam -= (int)Factor;
    }

    public override void RenewBuff(long duration)
    {
        base.RenewBuff(duration);
        Master.MoveSpeedParam += (int)Factor;
        TriggerBuff();
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.MoveSpeedParam += (int)Factor;
    }
}

public class Curse : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.Curse;
        Type = BuffType.Debuff;
    }
    
    public override void TriggerBuff()
    {
        BuffManager.Instance.Room?.SpawnEffect(EffectId.StateCurse, Master, Master.PosInfo, true, (int)Duration);
    }
    
    public override void RemoveBuff()
    {
        base.RemoveBuff();
        if (Master.Invincible || Master.Hp <= 0) return;
        Master.Hp = 1;
    }
}

public class Addicted : Buff
{
    private readonly double _dot = 1000;
    private double _dotTime;
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.Addicted;
        Type = BuffType.Debuff;
        _dotTime = StartTime;
        if (Math.Abs(param) - 0.001 < 0) Param = 0.05f;
    }

    public override void CalculateFactor()
    {
        if (ParamType == BuffParamType.Constant) Factor = Param;
        else Factor = Math.Clamp(Master.MaxHp * Param * (100 - Master.TotalPoisonResist) / 100, 0, 10000);
    }
    
    public override void TriggerBuff()
    {
        BuffManager.Instance.Room?.SpawnEffect(EffectId.StatePoison, Master, Master.PosInfo, true, (int)Duration);
    }

    public override bool UpdateBuff(long deltaTime)
    {
        if (EndTime <= deltaTime) return true;
        if (!(deltaTime > _dotTime + _dot)) return false;
        _dotTime = deltaTime;
        CalculateFactor();
        Master.OnDamaged(Caster, (int)Factor, Damage.Poison);

        return false;
    }
}

public class Aggro : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.Addicted;
        Type = BuffType.Debuff;
    }

    public override void TriggerBuff()
    {
        Master.Target = Caster;
        BuffManager.Instance.Room?.SpawnEffect(EffectId.StateAggro, Master, Master.PosInfo, true, (int)Duration);
    }
}

public class Burn : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.Burn;
        Type = BuffType.Debuff;
    }
    
    public override void TriggerBuff()
    {
        Master.Burn = true;
        BuffManager.Instance.Room?.SpawnEffect(EffectId.StateBurn, Master, Master.PosInfo, true, (int)Duration);
    }

    public override void RemoveBuff()
    {
        base.RemoveBuff();
        Master.Burn = false;
    }
}

public class Fainted : Buff
{
    public override void Init(BuffParamType paramType, 
        GameObject master, Creature caster, float param, long duration = 5000, bool nested = false)
    {
        base.Init(paramType, master, caster, param, duration, nested);
        Id = BuffId.Fainted;
        Type = BuffType.Debuff;
    }
    
    public override void TriggerBuff()
    {
        if (Master is not Creature creature) return;
        creature.OnFaint();
        BuffManager.Instance.Room?.SpawnEffect(EffectId.StateFaint, creature, creature.PosInfo, true, (int)Duration);
    }
    
    public override void RemoveBuff()
    {
        base.RemoveBuff();
        if (Master.Targetable == false || Master.Hp <= 0) return;
        Master.State = State.Idle;
    }
}
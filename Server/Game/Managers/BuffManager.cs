using System.Diagnostics;
using System.Runtime.Serialization;
using Google.Protobuf.Protocol;

namespace Server.Game;

public sealed class BuffManager
{
    public static BuffManager Instance { get; } = new();

    private const int CallCycle = 200;
    private IJob _job;
    private readonly object _lock = new();
    private readonly Stopwatch _stopwatch;
    private readonly Dictionary<BuffId, Type?> _buffDict;
    public List<IBuff> Buffs = new();
    public GameRoom? Room;
    
    private BuffManager()
    {
        _buffDict = new Dictionary<BuffId, Type?>
        {
            { BuffId.AttackIncrease, typeof(AttackBuff) },
            { BuffId.AttackSpeedIncrease, typeof(AttackSpeedBuff) },
            { BuffId.HealthIncrease, typeof(HealthBuff) },
            { BuffId.DefenceIncrease, typeof(DefenceBuff) },
            { BuffId.MoveSpeedIncrease, typeof(MoveSpeedBuff) },
            { BuffId.Invincible, typeof(Invincible) },
            { BuffId.AttackDecrease, typeof(AttackDebuff) },
            { BuffId.AttackSpeedDecrease, typeof(AttackSpeedDebuff) },
            { BuffId.DefenceDecrease, typeof(DefenceDebuff) },
            { BuffId.MoveSpeedDecrease, typeof(MoveSpeedDebuff) },
            { BuffId.Curse, typeof(Curse) },
            { BuffId.Addicted, typeof(Addicted) },
            { BuffId.DeadlyAddicted, typeof(DeadlyAddicted) },
            { BuffId.Aggro, typeof(Aggro) },
            { BuffId.Burn, typeof(Burn) }
        };
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public void AddBuff(BuffId buffId, Creature master, float param, long duration = 10000)
    {
        if (_buffDict.TryGetValue(buffId, out var type))
        {
            long addBuffTime = _stopwatch.ElapsedMilliseconds;
            object[] constArgs = { master, addBuffTime, duration, param };
            IBuff buff = (IBuff)Activator.CreateInstance(type!, constArgs)!;

            if (Buffs.Count == 0)
            {
                Buffs.Add(buff);
                buff.TriggerBuff();
            }
            else
            {
                foreach (var b in Buffs)
                {
                    ABuff oldBuff = (ABuff)b;
                    ABuff newBuff = (ABuff)buff;
                    if (oldBuff.Master == newBuff.Master && oldBuff.Nested == false && oldBuff.Id == newBuff.Id)
                    {
                        oldBuff.RenewBuff(addBuffTime);
                    }
                    else
                    {
                        Buffs.Add(newBuff);
                        newBuff.TriggerBuff();
                    }
                }
            }
        }
    }    
    
    public void AddBuff(BuffId buffId, GameObject master, float param, long duration = 10000)
    {
        if (_buffDict.TryGetValue(buffId, out var type))
        {
            long addBuffTime = _stopwatch.ElapsedMilliseconds;
            object[] constArgs = { master, addBuffTime, duration, param };
            IBuff buff = (IBuff)Activator.CreateInstance(type!, constArgs)!;

            if (Buffs.Count == 0)
            {
                Buffs.Add(buff);
                buff.TriggerBuff();
            }
            else
            {
                foreach (var b in Buffs)
                {
                    ABuff oldBuff = (ABuff)b;
                    ABuff newBuff = (ABuff)buff;
                    if (oldBuff.Master == newBuff.Master && oldBuff.Nested == false && oldBuff.Id == newBuff.Id)
                    {
                        oldBuff.RenewBuff(addBuffTime);
                    }
                    else
                    {
                        Buffs.Add(newBuff);
                        newBuff.TriggerBuff();
                    }
                }
            }
        }
    }
    
    public void AddBuff(BuffId buffId, Creature master, Creature caster, float param, long duration = 10000)
    {
        if (_buffDict.TryGetValue(buffId, out var type))
        {
            long addBuffTime = _stopwatch.ElapsedMilliseconds;
            object[] constArgs = { master, caster, addBuffTime, duration, param };
            IBuff buff = (IBuff)Activator.CreateInstance(type!, constArgs)!;

            if (Buffs.Count == 0)
            {
                Buffs.Add(buff);
                buff.TriggerBuff();
            }
            else
            {
                foreach (var b in Buffs)
                {
                    ABuff oldBuff = (ABuff)b;
                    ABuff newBuff = (ABuff)buff;
                    if (oldBuff.Master == newBuff.Master && oldBuff.Nested == false && oldBuff.Id == newBuff.Id)
                    {
                        oldBuff.RenewBuff(addBuffTime);
                    }
                    else
                    {
                        Buffs.Add(newBuff);
                        newBuff.TriggerBuff();
                    }
                }
            }
        }
    }    

    public void RemoveBuff(BuffId buffId, Creature master)
    {
        
    }

    public void RemoveAllBuff(Creature master)
    {
        List<IBuff> removeBuff = (from buff in Buffs let b = buff as ABuff
            where b?.Master.Id == master.Id select buff).ToList();

        if (removeBuff.Count != 0)
        {
            foreach (var buff in removeBuff)
            {
                buff.RemoveBuff();
                Buffs.Remove(buff);
            }
        }
    }

    public void Update()
    {
        if (Room != null) _job = Room.PushAfter(CallCycle, Update);
        if (Buffs.Count == 0) return;
        
        List<IBuff> expiredBuff = Buffs.Where(buff => buff.UpdateBuff(_stopwatch.ElapsedMilliseconds)).ToList();
        if (expiredBuff.Count != 0)
        {
            foreach (var buff in expiredBuff)
            {
                buff.RemoveBuff();
                Buffs.Remove(buff);
            }
        }
    }

    #region BuffClasses

    public interface IBuff
    {
        protected void CalculateFactor();
        public void TriggerBuff();
        public void RemoveBuff();
        public bool UpdateBuff(long deltaTime);
    }

    public abstract class ABuff : IBuff
    {
        public BuffId Id;
        protected BuffType Type;
        public Creature Master;
        protected long EndTime;
        private long _duration;
        public bool Nested;

        protected ABuff(Creature master, long startTime, long duration, float param)
        {
            Type = BuffType.NoBuffType;
            Master = master;
            _duration = duration;
            EndTime = startTime + _duration;
            Nested = false;
        }

        public virtual void CalculateFactor() { }
        public virtual void TriggerBuff() { }
        public virtual void RemoveBuff() { }

        public virtual void RenewBuff(long newEndTime, long duration = 10000)
        {
            _duration = duration;
            EndTime = newEndTime + _duration;
        }

        public virtual bool UpdateBuff(long deltaTime)
        {
            return EndTime <= deltaTime;
        }
    }
    
    private class AttackBuff : ABuff
    {
        private readonly float _param;
        private int _factor;
    
        public AttackBuff(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.AttackIncrease; 
            Type = BuffType.Buff;
            _param = param;
            CalculateFactor();
        }

        public sealed override void CalculateFactor()
        {
            _factor = (int)(Master.Attack * _param);
        }

        public override void TriggerBuff()
        {
            Master.TotalAttack += _factor;
        }

        public override void RemoveBuff()
        {
            Master.TotalAttack -= _factor;
        }
    }

    private class AttackSpeedBuff : ABuff
    {
        private readonly float _param;
        private float _factor;
        
        public AttackSpeedBuff(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.AttackSpeedIncrease;
            Type = BuffType.Buff;
            _param = param;
            CalculateFactor();
        }

        public sealed override void CalculateFactor()
        {
            _factor = Master.AttackSpeed * _param;
        }
        
        public override void TriggerBuff()
        {
            Master.TotalAttackSpeed += _factor;
        }

        public override void RemoveBuff()
        {
            Master.TotalAttackSpeed -= _factor;
        }
    }

    private class HealthBuff : ABuff
    {
        private readonly float _param;
        private int _factor;
        
        public HealthBuff(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.HealthIncrease;
            Type = BuffType.Buff;
            _param = param;
        }

        public sealed override void CalculateFactor()
        {
            _factor = (int)(Master.Hp * _param);
        }

        public override void TriggerBuff()
        {
            Master.MaxHp += _factor;
            Master.Hp += _factor;
        }

        public override void RemoveBuff()
        {
            Master.MaxHp -= _factor;
            if (Master.Hp > Master.MaxHp) Master.Hp = Master.MaxHp;
        }
    }
    
    private class DefenceBuff : ABuff
    {
        private readonly int _param;
        private int _factor;
        
        public DefenceBuff(Creature master, long startTime, long duration, int param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.DefenceIncrease;
            Type = BuffType.Buff;
            _param = param;
        }

        public sealed override void CalculateFactor()
        {
            _factor = Master.Defence + _param;
        }

        public override void TriggerBuff()
        {
            Master.TotalDefence += _factor;
        }

        public override void RemoveBuff()
        {
            Master.TotalDefence -= _factor;
        }
    }

    private class Invincible : ABuff
    {
        public Invincible(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.Invincible;
            Type = BuffType.Buff;
        }

        public override void TriggerBuff()
        {
            
        }
    }

    private class MoveSpeedBuff : ABuff
    {
        private readonly float _param;
        private float _factor;

        public MoveSpeedBuff(Creature master, long startTime, long duration, float param)
            : base(master, startTime, duration, param)
        {
            Id = BuffId.MoveSpeedIncrease;
            Type = BuffType.Buff;
            _param = param;
            CalculateFactor();
        }

        public sealed override void CalculateFactor()
        {
            _factor = Master.MoveSpeed * _param;
        }

        public override void TriggerBuff()
        {
            Master.TotalMoveSpeed += _factor;
        }

        public override void RemoveBuff()
        {
            Master.TotalMoveSpeed -= _factor;
        }
    }

    private class AttackDebuff : ABuff
    {
        private readonly float _param;
        private int _factor;
    
        public AttackDebuff(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.AttackDecrease;
            Type = BuffType.Debuff;
            _param = param;
            CalculateFactor();
        }

        public sealed override void CalculateFactor()
        {
            _factor = (int)(Master.Attack * _param);
        }

        public override void TriggerBuff()
        {
            Master.TotalAttack -= _factor;
        }

        public override void RemoveBuff()
        {
            Master.TotalAttack += _factor;
        }
    }

    private class AttackSpeedDebuff : ABuff
    {
        private readonly float _param;
        private float _factor;

        public AttackSpeedDebuff(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.AttackSpeedDecrease;
            Type = BuffType.Debuff;
            _param = param;
            CalculateFactor();
        }

        public sealed override void CalculateFactor()
        {
            _factor = Master.AttackSpeed * _param;
        }

        public override void TriggerBuff()
        {
            Master.TotalAttackSpeed -= _factor;
        }

        public override void RemoveBuff()
        {
            Master.TotalAttackSpeed += _factor;
        }
    }

    private class DefenceDebuff : ABuff
    {
        private readonly float _param;
        private int _factor;

        public DefenceDebuff(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.DefenceDecrease;
            Type = BuffType.Debuff;
            _param = param;
            CalculateFactor();
        }

        public sealed override void CalculateFactor()
        {
            _factor = (int)(Master.Defence * _param);
        }

        public override void TriggerBuff()
        {
            Master.TotalDefence -= _factor;
        }

        public override void RemoveBuff()
        {
            Master.TotalDefence += _factor;
        }
    }

    private class MoveSpeedDebuff : ABuff
    {
        private readonly float _param;
        private float _factor;
        
        public MoveSpeedDebuff(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.MoveSpeedDecrease;
            Type = BuffType.Debuff;
            _param = param;
        }

        public sealed override void CalculateFactor()
        {
            _factor = Master.MoveSpeed * _param;
        }

        public override void TriggerBuff()
        {
            Master.TotalMoveSpeed -= _factor;
        }

        public override void RemoveBuff()
        {
            Master.TotalMoveSpeed += _factor;
        }
    }

    private class Curse : ABuff
    {
        public Curse(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.Curse;
            Type = BuffType.Debuff;
            Nested = true;
        }

        public override void RemoveBuff()
        {
            Master.Hp = 1;
        }
    }
    
    private class Addicted : ABuff
    {
        private readonly float _param;
        private readonly double _dot = 1000;
        private double _dotTime = 0;
        
        public Addicted(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.Addicted;
            Type = BuffType.Debuff;
            _param = param;
        }

        public override bool UpdateBuff(long deltaTime)
        {
            if (EndTime <= deltaTime) return true;

            if (deltaTime > _dotTime + _dot)
            {
                _dotTime = deltaTime;
                Master.Hp -= (int)(Master.MaxHp * _param);
                Instance.Room?.Broadcast(new S_ChangeHp
                {
                    ObjectId = Master.Id,
                    Hp = Master.Hp
                });
            }

            return false;
        }
    }

    private class DeadlyAddicted : ABuff
    {
        private readonly float _param;
        private readonly double _dot = 1000;
        private double _dotTime = 0;
        
        public DeadlyAddicted(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.Addicted;
            Type = BuffType.Debuff;
            Nested = true;
            _param = param;
        }

        public override bool UpdateBuff(long deltaTime)
        {
            if (EndTime <= deltaTime) return true;

            if (deltaTime > _dotTime + _dot)
            {
                _dotTime = deltaTime;
                Master.Hp -= (int)(Master.MaxHp * _param);
                Instance.Room?.Broadcast(new S_ChangeHp
                {
                    ObjectId = Master.Id,
                    Hp = Master.Hp
                });
            }

            return false;
        }
    }

    public class Aggro : ABuff
    {
        private readonly Creature _caster;
        
        public Aggro(Creature master, Creature caster, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.Aggro;
            Type = BuffType.Debuff;
            Master = master;
            _caster = caster;
        }

        public override void TriggerBuff()
        {
            Master.Target = _caster;
        }

        public override void RemoveBuff()
        {
            
        }
    }
    
    private class Burn : ABuff
    {
        public new GameObject Master;
        private readonly float _param;
        private readonly double _dot = 1000;
        private double _dotTime = 0;
        
        public Burn(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.Addicted;
            Type = BuffType.Debuff;
            Master = master;
            _param = param;
        }

        public override void TriggerBuff()
        {
            
        }

        public override void RemoveBuff()
        {
            
        }

        public override bool UpdateBuff(long deltaTime)
        {
            if (EndTime <= deltaTime) return true;

            if (deltaTime > _dotTime + _dot && Master.ObjectType == GameObjectType.Fence)
            {
                _dotTime = deltaTime;
                Master.Hp -= (int)(Master.MaxHp * _param);
                Instance.Room?.Broadcast(new S_ChangeHp
                {
                    ObjectId = Master.Id,
                    Hp = Master.Hp
                });
            }

            return false;
        }
    }
    #endregion
}


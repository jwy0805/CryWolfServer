using System.Diagnostics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public sealed class BuffManager
{
    public static BuffManager Instance { get; } = new ();

    private const int CallCycle = 200;
    private IJob _job;
    private readonly object _lock = new();
    private readonly Stopwatch _stopwatch;
    private readonly Dictionary<BuffId, Type?> _buffDict;
    private List<IBuff> _buffs = new();
    public GameRoom? Room;
    
    private struct BuffInfo
    {
        public Creature Master;
        public IBuff Buff;

        public BuffInfo(Creature master, IBuff buff)
        {
            Master = master;
            Buff = buff;
        }
    }
    
    private BuffManager()
    {
        _buffDict = new Dictionary<BuffId, Type?>
        {
            { BuffId.AttackIncrease, typeof(AttackBuff) },
            { BuffId.AttackSpeedIncrease, typeof(AttackSpeedBuff) },
            { BuffId.MoveSpeedIncrease, typeof(MoveSpeedBuff) },
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

            if (_buffs.Count == 0)
            {
                _buffs.Add(buff);
                buff.TriggerBuff();
            }
            else
            {
                foreach (var b in _buffs)
                {
                    ABuff oldBuff = (ABuff)b;
                    ABuff newBuff = (ABuff)buff;
                    if (oldBuff.Master == newBuff.Master && oldBuff.Nested == false && oldBuff.Id == newBuff.Id)
                    {
                        oldBuff.RenewBuff(addBuffTime);
                    }
                    else
                    {
                        _buffs.Add(newBuff);
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
        List<IBuff> removeBuff = (from buff in _buffs let b = buff as ABuff
            where b?.Master.Id == master.Id select buff).ToList();

        if (removeBuff.Count != 0)
        {
            foreach (var buff in removeBuff)
            {
                buff.RemoveBuff();
                _buffs.Remove(buff);
            }
        }
    }

    public void Update()
    {
        if (Room != null) _job = Room.PushAfter(CallCycle, Update);
        if (_buffs.Count == 0) return;
        
        List<IBuff> expiredBuff = _buffs.Where(buff => buff.UpdateBuff(_stopwatch.ElapsedMilliseconds)).ToList();
        if (expiredBuff.Count != 0)
        {
            foreach (var buff in expiredBuff)
            {
                buff.RemoveBuff();
                _buffs.Remove(buff);
            }
        }
    }

    #region BuffClasses

    public interface IBuff
    {
        public void CalculateFactor();
        public void TriggerBuff();
        public void RemoveBuff();
        public bool UpdateBuff(long deltaTime);
    }

    private abstract class ABuff : IBuff
    {
        public BuffId Id;
        protected BuffType Type;
        public Creature Master;
        protected readonly long StartTime;
        protected long EndTime;
        protected long Duration;
        public bool Nested;

        protected ABuff(Creature master, long startTime, long duration, float param)
        {
            Type = BuffType.NoBuffType;
            Master = master;
            StartTime = startTime; 
            Duration = duration;
            EndTime = StartTime + Duration;
            Nested = false;
        }

        public virtual void CalculateFactor() { }
        public virtual void TriggerBuff() { }
        public virtual void RemoveBuff() { }

        public virtual void RenewBuff(long newEndTime, long duration = 10000)
        {
            Duration = duration;
            EndTime = newEndTime + Duration;
        }

        public virtual bool UpdateBuff(long deltaTime)
        {
            Console.WriteLine($"{EndTime}, {deltaTime}");
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
            Nested = false;
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
            Nested = false;
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
    
    private class DefenceBuff : ABuff
    {
        private readonly int _param;
        private int _factor;
        
        public DefenceBuff(Creature master, long startTime, long duration, int param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.DefenceIncrease;
            Type = BuffType.Buff;
            Nested = false;
            _param = param;
        }

        public sealed override void CalculateFactor()
        {
            _factor = Master.Defence + _param;
        }

        public override void TriggerBuff()
        {
            Master.Defence += _factor;
        }

        public override void RemoveBuff()
        {
            Master.Defence -= _factor;
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
        public float Param;
    
        public AttackDebuff(Creature master, long startTime, long duration, float param) 
            : base(master, startTime, duration, param)
        {
            Id = BuffId.AttackDecrease;
            Type = BuffType.Buff;
            Master = master;
            Param = param;
        }
    }
    
    #endregion
}


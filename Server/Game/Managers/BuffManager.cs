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
    
    private BuffManager()
    {
        _buffDict = new Dictionary<BuffId, Type?>
        {
            { BuffId.AttackIncrease, typeof(AttackBuff) },
        };
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public void AddBuff(BuffId buffId, Creature master, float param)
    {
        if (_buffDict.TryGetValue(buffId, out var type))
        {
            long time = _stopwatch.ElapsedMilliseconds;
            object[] constArgs = { master, param, time };
            IBuff buff = (IBuff)Activator.CreateInstance(type!, constArgs)!;
            buff.TriggerBuff();
            _buffs.Add(buff);
            master.BuffList.Add(buff);
        }
    }

    public void Update()
    {
        
        if (Room != null) _job = Room.PushAfter(CallCycle, Update);
    }
    
    public interface IBuff
    {
        public void TriggerBuff();
        public void RemoveBuff();
    }

    private abstract class ABuff : IBuff
    {
        public BuffType Type;
        public Creature Master;
        private long _startTime;

        protected ABuff(Creature master, long startTime, float duration, float param)
        {
            Type = BuffType.NoBuffType;
            Master = master;
        }

        public virtual void TriggerBuff() { }

        public virtual void RemoveBuff() { }
    }
    
    private class AttackBuff : ABuff
    {
        public float Param;
    
        public AttackBuff(Creature master, long startTime, float duration, float param) 
            : base(master, startTime, duration, param)
        {
            Type = BuffType.Buff;
            Master = master;
            Param = param;
        }

        public override void TriggerBuff()
        {
            Master.TotalAttack += (int)Param;
        }

        public override void RemoveBuff()
        {
            Master.TotalAttack -= (int)Param;
        }
    }

    private class AttackDebuff : ABuff
    {
        public float Param;
    
        public AttackDebuff(Creature master, long startTime, float duration, float param) 
            : base(master, startTime, duration, param)
        {
            Type = BuffType.Buff;
            Master = master;
            Param = param;
        }
    }
}


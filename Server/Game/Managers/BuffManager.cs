using System.Diagnostics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public sealed partial class BuffManager
{
    public static BuffManager Instance { get; } = new();

    private const int CallCycle = 200;
    private IJob _job;
    private readonly Stopwatch _stopwatch;
    private readonly Dictionary<BuffId, IBuffFactory> _buffDict = new() 
    {
        { BuffId.AttackIncrease, new AttackBuffFactory() },
        { BuffId.AttackSpeedIncrease, new AttackSpeedBuffFactory() },
        { BuffId.HealthIncrease, new HealthBuffFactory() },
        { BuffId.DefenceIncrease, new DefenceBuffFactory() },
        { BuffId.MoveSpeedIncrease, new MoveSpeedBuffFactory() },
        { BuffId.Invincible, new InvincibleFactory() },
        { BuffId.AttackDecrease, new AttackDebuffFactory() },
        { BuffId.AttackSpeedDecrease, new AttackSpeedDebuffFactory() },
        { BuffId.DefenceDecrease, new DefenceDebuffFactory() },
        { BuffId.MoveSpeedDecrease, new MoveSpeedDebuffFactory() },
        { BuffId.Curse, new CurseFactory() },
        { BuffId.Addicted, new AddictedFactory() },
        { BuffId.DeadlyAddicted, new DeadlyAddictedFactory() },
        { BuffId.Aggro, new AggroFactory() },
        { BuffId.Burn, new BurnFactory() },
        { BuffId.Fainted, new FaintedFactory() }
    };
    public HashSet<ABuff> Buffs = new();
    public GameRoom? Room;
    
    private BuffManager()
    {
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }
    
    public void AddBuff(BuffId buffId, GameObject master, Creature caster, float param, long duration = 10000, bool nested = false)
    {
        if (!_buffDict.TryGetValue(buffId, out var factory)) return;
        
        long addBuffTime = _stopwatch.ElapsedMilliseconds;
        ABuff buff = factory.CreateBuff();
        buff.Init(master, caster, addBuffTime, duration, param, nested);

        if (master.Invincible && buff.Type == BuffType.Debuff) return;
        if (buff.Nested == false && master.Buffs.Contains(buff.Id))
        {
            ABuff? b = Buffs.FirstOrDefault(b => b.Caster == caster && b.Id == buffId);
            b?.RenewBuff(addBuffTime);
        }
        else
        {
            master.Buffs.Add(buff.Id);
            Buffs.Add(buff);
            buff.TriggerBuff();
        }
    }

    public void RemoveBuff(BuffId buffId, Creature master)
    {
        
    }

    public void RemoveAllBuff(Creature master)
    {
        List<ABuff> removeBuff = (from buff in Buffs 
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
        List<ABuff> removeDebuff = (from buff in Buffs 
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
        
        List<ABuff> expiredBuff = Buffs.Where(buff => buff.UpdateBuff(_stopwatch.ElapsedMilliseconds)).ToList();
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
        public BuffType Type;
        public GameObject Master;
        public Creature Caster;
        protected long EndTime;
        private long _duration;
        public bool Nested;

        public virtual void Init(GameObject master, Creature caster, long startTime, long duration, float param, bool nested)
        {
            Type = BuffType.NoBuffType;
            Master = master;
            Caster = caster;
            _duration = duration;
            EndTime = startTime + _duration;
            Nested = nested;
        }
        public virtual void CalculateFactor() { }
        public virtual void TriggerBuff() { }
        public virtual bool UpdateBuff(long deltaTime)
        {
            return EndTime <= deltaTime;
        }
        public virtual void RenewBuff(long newStartTime, long duration = 10000)
        {
            _duration = duration;
            EndTime = newStartTime + _duration;
            RemoveBuff();
            CalculateFactor();
            TriggerBuff();
        }
        public virtual void RemoveBuff()
        {
            Master.Buffs.Remove(Id);
            ABuff? buff = Instance.Buffs.FirstOrDefault(b => b.Master == Master && b.Id == Id);
            if (buff != null) Instance.Buffs.Remove(buff);
        }
        protected virtual void EffectSetting(Effect effect, GameObject master)
        {
            effect.Room = Instance.Room;
            effect.Target = master;
            effect.PosInfo = master.PosInfo;
            effect.Info.PosInfo = master.Info.PosInfo;
            effect.Init();
            effect.Info.Name = effect.EffectId.ToString();
            Instance.Room?.EnterGameParent(effect, master);
        }
    }
    
    public class AttackBuff : ABuff
    {
        private int _factor;
        private float _param;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.AttackIncrease; 
            Type = BuffType.Buff;
            _param = param;
            CalculateFactor();
        }

        public sealed override void CalculateFactor()
        {
            if (Master.Burn) _factor = (int)(Master.Attack * _param * Master.FireResist * 0.01f);
            else _factor = (int)(Master.Attack * _param);
        }

        public override void TriggerBuff()
        {
            Master.Attack += _factor;
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.Attack -= _factor;
        }
    }

    private class AttackSpeedBuff : ABuff
    {
        private float _param;
        private float _factor;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.AttackSpeedIncrease;
            Type = BuffType.Buff;
            _param = param;
            CalculateFactor();        
        }

        public sealed override void CalculateFactor()
        {
            if (Master.Burn) _factor = Master.AttackSpeed * _param * Master.FireResist * 0.01f;
            else _factor = Master.AttackSpeed * _param;
        }
        
        public override void TriggerBuff()
        {
            Master.AttackSpeed += _factor;
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.AttackSpeed -= _factor;
        }
    }

    private class HealthBuff : ABuff
    {
        private float _param;
        private int _factor;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.HealthIncrease;
            Type = BuffType.Buff;
            _param = param;        
        }

        public sealed override void CalculateFactor()
        {
            if (Master.Burn) _factor = (int)(Master.Hp * _param * Master.FireResist * 0.01f);
            else _factor = (int)_param;
        }

        public override void TriggerBuff()
        {
            Master.MaxHp += _factor;
            Master.Hp += _factor;
            Master.BroadcastHealth();
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.MaxHp -= _factor;
            Master.BroadcastHealth();
            if (Master.Hp > Master.MaxHp) Master.Hp = Master.MaxHp;
        }
    }
    
    private class DefenceBuff : ABuff
    {
        private int _param;
        private int _factor;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.DefenceIncrease;
            Type = BuffType.Buff;
            _param = (int)param;        
        }

        public sealed override void CalculateFactor()
        {
            if (Master.Burn) _factor = (int)((Master.Defence + _param) * Master.FireResist * 0.01f);
            else _factor = Master.Defence + _param;
        }

        public override void TriggerBuff()
        {
            Master.Defence += _factor;
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.Defence -= _factor;
        }
    }

    private class Invincible : ABuff
    {
        private readonly Effect _holyAura = ObjectManager.Instance.CreateEffect(EffectId.HolyAura);
        
        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.Invincible;
            Type = BuffType.Buff;        
        }

        public override void TriggerBuff()
        {
            Master.Invincible = true;
            EffectSetting(_holyAura, Master);
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.Invincible = false;
            _holyAura.PacketReceived = true;
        }
    }

    private class MoveSpeedBuff : ABuff
    {
        private float _param;
        private float _factor;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.MoveSpeedIncrease;
            Type = BuffType.Buff;
            _param = param;
            CalculateFactor();        
        }

        public sealed override void CalculateFactor()
        {
            if (Master.Burn) _factor = Master.MoveSpeed * _param * Master.FireResist * 0.01f;
            else _factor = Master.MoveSpeed * _param;
        }

        public override void TriggerBuff()
        {
            Master.MoveSpeed += _factor;
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.MoveSpeed -= _factor;
        }
    }

    private class AttackDebuff : ABuff
    {
        private float _param;
        private int _factor;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
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
            Master.Attack -= _factor;
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.Attack += _factor;
        }
    }

    private class AttackSpeedDebuff : ABuff
    {
        private float _param;
        private float _factor;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
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
            Master.AttackSpeed -= _factor;
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.AttackSpeed += _factor;
        }
    }

    private class DefenceDebuff : ABuff
    {
        private float _param;
        private int _factor;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
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
            Master.Defence -= _factor;
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.Defence += _factor;
        }
    }

    private class MoveSpeedDebuff : ABuff
    {
        private float _param;
        private float _factor;
        private Effect? _stateSlow;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
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
            Master.MoveSpeed -= _factor;
            _stateSlow = ObjectManager.Instance.CreateEffect(EffectId.StateSlow);
            EffectSetting(_stateSlow, Master);
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.MoveSpeed += _factor;
            if (_stateSlow != null) _stateSlow.PacketReceived = true;
        }
    }

    private class Curse : ABuff
    {
        private Effect? _stateCurse;
        
        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.Curse;
            Type = BuffType.Debuff;
            Nested = true;
        }

        public override void TriggerBuff()
        {
            _stateCurse = ObjectManager.Instance.CreateEffect(EffectId.StateCurse);
            EffectSetting(_stateCurse, Master);
        }
        
        public override void RemoveBuff()
        {
            base.RemoveBuff();
            if (Master.Invincible) return; 
            Master.Hp = 1;
            Instance.Room?.Broadcast(new S_ChangeHp
            {
                ObjectId = Master.Id,
                Hp = Master.Hp
            });
            if (_stateCurse != null) _stateCurse.PacketReceived = true;
        }
    }
    
    private class Addicted : ABuff
    {
        private float _param;
        private readonly double _dot = 1000;
        private double _dotTime = 0;
        private Effect? _statePoison;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.Addicted;
            Type = BuffType.Debuff;
            _param = 0.05f;
            _dotTime = startTime;
        }

        public override void TriggerBuff()
        {
            _statePoison = ObjectManager.Instance.CreateEffect(EffectId.StatePoison);
            EffectSetting(_statePoison, Master);
        }
        
        public override bool UpdateBuff(long deltaTime)
        {
            if (EndTime <= deltaTime) return true;
            if (Master.Invincible) return false; 

            if (deltaTime > _dotTime + _dot)
            {
                _dotTime = deltaTime;
                Master.Hp -= (int)(Master.MaxHp * _param * (100 - Master.PoisonResist) / 100);
                Instance.Room?.Broadcast(new S_ChangeHp
                {
                    ObjectId = Master.Id,
                    Hp = Master.Hp
                });
            }

            return false;
        }
        
        public override void RemoveBuff()
        {
            base.RemoveBuff();
            if (_statePoison != null) _statePoison.PacketReceived = true;
        }
    }

    private class DeadlyAddicted : ABuff
    {
        private float _param;
        private readonly double _dot = 1000;
        private double _dotTime = 0;
        private Effect? _statePoison;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.Addicted;
            Type = BuffType.Debuff;
            Nested = true;
            _param = param;
        }

        public override void TriggerBuff()
        {
            _statePoison = ObjectManager.Instance.CreateEffect(EffectId.StatePoison);
            EffectSetting(_statePoison, Master);
        }
        
        public override bool UpdateBuff(long deltaTime)
        {
            if (EndTime <= deltaTime) return true;
            if (Master.Invincible) return false; 
            
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
        
        public override void RemoveBuff()
        {
            base.RemoveBuff();
            if (_statePoison != null) _statePoison.PacketReceived = true;
        }
    }

    public class Aggro : ABuff
    {
        private Creature? _caster;
        private Effect? _stateAggro;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.Aggro;
            Type = BuffType.Debuff;
            Master = master;
            _caster = caster;
        }

        public override void TriggerBuff()
        {
            if (Master.Invincible) return; 
            Master.Target = _caster;
            _stateAggro = ObjectManager.Instance.CreateEffect(EffectId.StateAggro);
            EffectSetting(_stateAggro, Master);
        }
        
        public override void RemoveBuff()
        {
            base.RemoveBuff();
            if (_stateAggro != null) _stateAggro.PacketReceived = true;
        }
    }
    
    private class Burn : ABuff
    {
        private float _param;
        private readonly double _dot = 1000;
        private double _dotTime = 0;
        private Effect? _stateBurn;

        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.Addicted;
            Type = BuffType.Debuff;
            Master = master;
            _param = param;
        }

        public override void TriggerBuff()
        {
            Master.Burn = true;
            _stateBurn = ObjectManager.Instance.CreateEffect(EffectId.StateBurn);
        }

        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.Burn = false;
            if (_stateBurn != null) _stateBurn.PacketReceived = true;
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
    
    private class Fainted : ABuff
    {
        private Effect? _stateFaint;
        
        public override void Init(GameObject master, Creature caster, long startTime, long duration, float param,
            bool nested)
        {
            base.Init(master, caster, startTime, duration, param, nested);
            Id = BuffId.Fainted;
            Type = BuffType.Debuff;
            Nested = false;
        }
        
        public override void TriggerBuff()
        {
            Master.State = State.Faint;
            Master.BroadcastMove();
            _stateFaint = ObjectManager.Instance.CreateEffect(EffectId.StateFaint);
            EffectSetting(_stateFaint, Master);
        }
        
        public override void RemoveBuff()
        {
            base.RemoveBuff();
            Master.State = State.Idle;
            Master.BroadcastMove();
            if (_stateFaint != null) _stateFaint.PacketReceived = true;
        }
    }
    
    
    #endregion
}


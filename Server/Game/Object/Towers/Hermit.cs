using Google.Protobuf.Protocol;

namespace Server.Game;

public class Hermit : Spike
{
    private bool _normalAttackDefence;
    private bool _reflectionFaint;
    private bool _recoverBurn;
    private bool _shield;
    private int _damageRemainder;
    private Guid _skillEndTaskId;
    private readonly float _recoverBurnRange = 3f;
    private readonly int _reflectionFaintRate = 7;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HermitNormalAttackDefence:
                    _normalAttackDefence = true;
                    break;
                case Skill.HermitAttackerFaint:
                    _reflectionFaint = true;
                    break;
                case Skill.HermitRecoverBurn:
                    _recoverBurn = true;
                    break;
                case Skill.HermitShield:
                    _shield = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
        SkillImpactMoment = 0.3f;
    }
    
    public override void Update() 
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            if (Mp >= MaxMp && _normalAttackDefence)
            {
                State = State.Skill;
                return;
            }
        }

        switch (State)
        {
            case State.Die:
                UpdateDie();
                break;
            case State.Moving:
                UpdateMoving();
                break;
            case State.Idle:
                UpdateIdle();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Skill:
                UpdateSkill();
                break;
            case State.KnockBack:
                UpdateKnockBack();
                break;
            case State.Faint:
            case State.Standby:
                break;
        }
    }
    
    protected override void UpdateSkill() { }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.HermitProjectile, this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        if (Room == null) return;
        if (_recoverBurn)
        {
            Room.SpawnEffect(EffectId.HermitRecoverBurn, this, PosInfo, true, 2700);
            var types = new[] { GameObjectType.Tower };
            var targets = Room.FindTargets(this, types, _recoverBurnRange);
            foreach (var target in targets.Select(target => target as Creature))
            {
                if (target is null || target.Hp <= 0) continue;
                Room.Push(Room.RemoveBuff, BuffId.Burn, target);
            }
        }

        Mp = 0;
    }

    private void SkillEndEvents(long impactTime)
    {
        _skillEndTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            AttackEnded = true;
            State = State.Idle;
            if (_shield)
            {
                ShieldAdd = _damageRemainder;
                _damageRemainder = 0;
            }
        });
    }
    
    protected override void SetNextState()
    {
        if (Room == null || State == State.Skill) return;
        base.SetNextState();
    }

    protected override void OnSkill()
    {
        if (Room == null || Hp <= 0) return;
        
        SkillImpactEvents(300);
        SkillEndEvents(3000);
    }
    
    public override void OnFaint()
    {
        State = State.Faint;
        AttackEnded = true;
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
        Scheduler.CancelEvent(_skillEndTaskId);
        _damageRemainder = 0;
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null || AddBuffAction == null) return;
        if (Invincible || Targetable == false || Hp <= 0) return;
        var random = new Random();

        if (State == State.Skill && damageType == Damage.Normal)
        {   // Normal Attack Defence
            _damageRemainder += damage;
            damage = 0;
        }
        
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {   // Evasion
            // TODO: Evasion Effect
            return;
        }
        
        var totalDamage = random.Next(100) < attacker.CriticalChance && damageType is Damage.Normal
            ? (int)(damage * attacker.CriticalMultiplier) : damage;
        
        if (ShieldRemain > 0)
        {   // Shield
            ShieldRemain -= totalDamage;
            if (ShieldRemain < 0)
            {
                totalDamage = Math.Abs(ShieldRemain);
                ShieldRemain = 0;
            }
        }

        totalDamage = damageType is Damage.Normal or Damage.Magical
            ? Math.Max(totalDamage - TotalDefence, 0) : damage;
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        
        if (Hp <= 0)
        {
            OnDead(attacker);
            return;
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false && attacker.Targetable)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            Room.Push(attacker.OnDamaged, this, reflectionDamage, damageType, true);
            if (_reflectionFaint && new Random().Next(100) < _reflectionFaintRate && attacker.Targetable)
            {
                Room.Push(AddBuffAction, BuffId.Fainted,
                    BuffParamType.None, attacker, this, 0, 1000, false);
            }
        }
    }
}
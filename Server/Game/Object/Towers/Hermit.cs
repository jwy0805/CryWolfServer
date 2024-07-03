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
        Player.SkillSubject.SkillUpgraded(Skill.HermitNormalAttackDefence);
        Player.SkillSubject.SkillUpgraded(Skill.HermitAttackerFaint);
        Player.SkillSubject.SkillUpgraded(Skill.HermitRecoverBurn);
    }
    
    public override void Update() 
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            if (Mp >= MaxMp && _normalAttackDefence)
            {
                State = State.Skill;
                OnSkill();
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
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
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
                BuffManager.Instance.RemoveBuff(BuffId.Burn, target);
            }
        }

        Mp = 0;
    }

    private void SkillEndEvents(long impactTime)
    {
        _skillEndTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            State = State.Idle;
        });
    }
    
    protected override void SetNextState()
    {
        if (Room == null || State == State.Skill) return;
        base.SetNextState();
    }

    private void OnSkill()
    {
        SkillImpactEvents(300);
        SkillEndEvents(3000);
        IsAttacking = false;
        AttackEnded = true;
    }
    
    public override void OnFaint()
    {
        State = State.Faint;
        IsAttacking = false;
        AttackEnded = true;
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
        Scheduler.CancelEvent(_skillEndTaskId);
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible || Targetable == false || Hp <= 0) return;
        // Normal Attack Defence
        if (State == State.Skill && damageType == Damage.Normal)
        {
            _damageRemainder += damage;
            damage = 0;
        }
        
        var random = new Random();
        var totalDamage = damageType is Damage.Normal or Damage.Magical 
            ? Math.Max(damage - TotalDefence, 0) : damage;
        
        if (random.Next(100) < attacker.CriticalChance)
        {
            totalDamage = (int)(totalDamage * attacker.CriticalMultiplier);
        }
        
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {
            // TODO: Evasion Effect
            return;
        }
        
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
            attacker.OnDamaged(this, reflectionDamage, damageType, true);
            if (_reflectionFaint && new Random().Next(100) < _reflectionFaintRate && attacker.Targetable)
            {
                BuffManager.Instance.AddBuff(BuffId.Fainted, BuffParamType.None, 
                    attacker, this, 0, 1000);            }
        }
    }
}
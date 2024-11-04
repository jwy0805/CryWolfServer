using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SoulMage : Haunt
{
    private bool _dragonPunch = false;
    private bool _shareDamage = false;
    private bool _magicPortal = false;
    private bool _debuffResist = false;
    private readonly int _debuffParam = 25;
    private GameObject? _effectTarget;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SoulMageDragonPunch:
                    _dragonPunch = true;
                    break;
                case Skill.SoulMageShareDamage:
                    _shareDamage = true;
                    break;
                case Skill.SoulMageMagicPortal:
                    _magicPortal = true;
                    break;
                case Skill.SoulMageDebuffResist:
                    _debuffResist = true;
                    break;
                case Skill.SoulMageCritical:
                    CriticalChance += 25;
                    CriticalMultiplier = 1.5f;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            
            if (Mp >= MaxMp && _magicPortal)
            {
                var effectPos = new PositionInfo
                { 
                    PosX = CellPos.X, PosY = CellPos.Y + 4, PosZ = CellPos.Z, Dir = Dir
                };
                Room.SpawnEffect(EffectId.GreenGate, this, this, effectPos, false, 3500);
                Mp = 0;
                return;
            }
        }

        switch (State)
        {
            case State.Die:
                UpdateDie();
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
            case State.Rush:
                UpdateRush();
                break;
            case State.Faint:
                break;
            case State.Standby:
                break;
        }
    }
    
    protected override void UpdateIdle()
    {   
        // Targeting
        Target = Room?.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 flatTargetPos = Target.CellPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);

        if (distance > TotalAttackRange) return;
        State = _dragonPunch ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
    
    protected override void OnSkill()
    {
        if (Target == null || Target.Targetable == false) return;
        base.OnSkill();
        AttackEnded = false;
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {   
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.SoulMageProjectile, this, 5f);
        });
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            
            var effectPos = new PositionInfo
            { 
                PosX = CellPos.X, PosY = CellPos.Y, PosZ = CellPos.Z, Dir = Dir
            };
                
            Room?.SpawnEffect(EffectId.SoulMagePunch, this, this, effectPos);
        });
    }

    private async void NaturalTornadoEvents(long impactTime)
    {
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (_effectTarget == null || Room == null || AddBuffAction == null) return;
            Room.Push(AddBuffAction, BuffId.Fainted,
                BuffParamType.None, _effectTarget, this, 0, 1300, false);
            Room.Push(_effectTarget.OnDamaged, this, (int)(TotalSkillDamage * 0.4), Damage.True, false);
        });
    }
    
    private async void StarFallEvents(long impactTime, PositionInfo effectPos)
    {
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Room == null) return;
            
            var types = new[] { GameObjectType.Monster, GameObjectType.MonsterStatue };
            var effectCellPos = new Vector3(effectPos.PosX, effectPos.PosY, effectPos.PosZ);
            var targets = Room.FindTargets(effectCellPos, types, 3.5f, 2);
            if (targets.Any() == false) return;
            
            foreach (var target in targets)
            {
                Room.Push(target.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
            }
        });
    }
    
    private async void PurpleBeamEvents(long impactTime)
    {
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Room == null || _effectTarget == null) return;
            Room.Push(_effectTarget.OnDamaged, this, (int)(TotalSkillDamage * 0.7), Damage.Magical, false);
        });
    }
    
    public override void ApplyEffectEffect()
    {
        if (Room == null || AddBuffAction == null) return;

        float dir;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            dir = Dir;
        }
        else
        {
            double deltaX = Target.CellPos.X - CellPos.X;
            double deltaZ = Target.CellPos.Z - CellPos.Z;
            dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        }
        
        var types = new[] { GameObjectType.Monster, GameObjectType.MonsterStatue };
        var targets = Room.FindTargetsInRectangle(this,
            types, 2, 7, dir, 2);
        foreach (var target in targets)
        {
            Room.Push(target.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
            Room.Push(AddBuffAction, BuffId.Fainted, BuffParamType.None, target, this, 0, 1300, false);
        }
    }

    public override void ApplyEffectEffect(EffectId eid)
    {
        if (Room == null) return;
        if (eid != EffectId.GreenGate) return;
        
        var random = new Random().Next(3);
        var types = new List<GameObjectType> { GameObjectType.Monster };
        _effectTarget = Room.FindRandomTarget(this, types, TotalSkillRange, 2);
        if (_effectTarget == null)
        {   // 몬스터가 우선순위 1이므로 먼저 찾고 그 다음 석상 탐색
            types = new List<GameObjectType> { GameObjectType.MonsterStatue };
            _effectTarget = Room.FindRandomTarget(this, types, TotalSkillRange, 2);
            if (_effectTarget == null) return;
        }
                
        var effectPos = new PositionInfo
        { 
            PosX = _effectTarget.CellPos.X, PosY = _effectTarget.CellPos.Y, PosZ = _effectTarget.CellPos.Z, Dir = Dir
        };
                
        switch (random)
        {
            case 0:
                Room.SpawnEffect(EffectId.NaturalTornado, this, _effectTarget, effectPos, true, 3000);
                NaturalTornadoEvents(100);
                NaturalTornadoEvents(800);
                NaturalTornadoEvents(1500);
                NaturalTornadoEvents(2200);
                break;
            case 1:
                Room.SpawnEffect(EffectId.StarFall, this, this, effectPos, false, 3000);
                StarFallEvents(500, effectPos);
                StarFallEvents(1000, effectPos);
                StarFallEvents(1500, effectPos);
                break;
            default:
                Room.SpawnEffect(EffectId.PurpleBeam, this, _effectTarget, effectPos, true, 4000);
                PurpleBeamEvents(900);
                PurpleBeamEvents(1150);
                PurpleBeamEvents(1400);
                PurpleBeamEvents(1650);
                PurpleBeamEvents(1900);
                PurpleBeamEvents(2150);
                PurpleBeamEvents(2400);
                PurpleBeamEvents(2650);
                PurpleBeamEvents(2900);
                break;
        }
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible || Targetable == false || Hp <= 0) return;
        
        var random = new Random();
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {   // Evasion
            // TODO: Evasion Effect
            return;
        }
        
        // 일반적으로 Normal Damage 만 Critical 가능, Magical이나 True Damage Critical 구현 시 데미지를 넣는 Unit으로부터 자체적으로 계산
        var totalDamage = random.Next(100) < attacker.CriticalChance && damageType is Damage.Normal
            ? (int)(damage * attacker.CriticalMultiplier) : damage;

        if (_shareDamage)
        {
            var type = new[] { GameObjectType.Tower };
            var tanker = Room.FindTargets(this, type, 100, 2)
                .Where(gameObject => gameObject is Creature { UnitRole: Role.Tanker, Hp: > 0, Targetable: true })
                .MinBy(tanker => Vector3.Distance(tanker.CellPos, CellPos));
            
            if (tanker != null)
            {
                var halfDamage = (int)(totalDamage * 0.5f);
                Room.Push(tanker.OnDamaged, attacker, halfDamage, damageType, false);
                totalDamage = halfDamage;
            }
        }
        
        if (ShieldRemain > 0)
        {   
            // Shield
            ShieldRemain -= totalDamage;
            if (ShieldRemain < 0)
            {
                totalDamage = Math.Abs(ShieldRemain);
                ShieldRemain = 0;
            }
        }

        totalDamage = GameManager.Instance.CalcDamage(this, damageType, totalDamage);
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
        }
    }

    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0 || Target.Room == null)
        {
            State = State.Idle;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        if (distance > TotalAttackRange)
        {
            State = State.Idle;
            return;
        }

        State = _dragonPunch ? State.Skill : State.Attack;
        SyncPosAndDir();
    }

    public override void AddBuff(Buff buff)
    {
        if (_debuffResist && buff.Type is BuffType.Debuff)
        {
            if (new Random().Next(100) < _debuffParam) return;
        }
        
        base.AddBuff(buff);
    }
}
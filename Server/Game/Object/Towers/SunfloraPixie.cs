using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SunfloraPixie : SunflowerFairy
{
    private bool _recoverMp;
    private bool _strongAttack;
    private bool _invincible;
    private bool _debuffRemove;
    private bool _triple;
    private readonly int _mpRecoverParam = 40;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SunfloraPixieRecoverMp:
                    _recoverMp = true;
                    break;
                case Skill.SunfloraPixieStrongAttack:
                    _strongAttack = true;
                    break;
                case Skill.SunfloraPixieInvincible:
                    _invincible = true;
                    break;
                case Skill.SunfloraPixieDebuffRemove:
                    _debuffRemove = true;
                    break;
                case Skill.SunfloraPixieTripleBuff:
                    _triple = true;
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        UnitRole = Role.Supporter;
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            
            // Heal
            if (AddBuffAction == null) return;
            var num = _triple ? 3 : 2;
            var types = new[] { GameObjectType.Tower };
            var skillTargets = Room.FindTargets(this, types, TotalSkillRange, AttackType);
            var healTargets = skillTargets
                .OrderBy(target => target.Hp / target.MaxHp).Take(num).ToList();
            foreach (var target in healTargets)
            {
                Room.Push(AddBuffAction, BuffId.HealBuff,
                    BuffParamType.Constant, target, this, HealParam, 1000, false);
                // Debuff Remove
                if (_debuffRemove && target is Creature creature) Room.Push(Room.RemoveAllDebuffs, creature);
            }
            
            // Fence Heal
            var fenceType = new[] { GameObjectType.Fence };
            var fenceTargets = Room.FindTargets(this, fenceType, TotalSkillRange, AttackType)
                .OrderBy(target => target.Hp).Take(num).ToList();
            foreach (var target in fenceTargets)  
            {
                Room.Push(AddBuffAction, BuffId.HealBuff,
                    BuffParamType.Constant, target, this, FenceHealParam, 1000, false);
            }
            
            if (Mp >= MaxMp)
            {
                State = State.Skill;
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
            case State.Revive:
            case State.Faint:
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

        if (distance <= TotalAttackRange)
        {
            State = State.Attack;
            SyncPosAndDir();
        }
        else
        {
            // Target = null;
        }
    }
    
    protected override void UpdateAttack()
    {
        if (Room == null) return;
        
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            if (AttackEnded) return;
            AttackEnded = true;
            Scheduler.CancelEvent(AttackTaskId);
            Scheduler.CancelEvent(EndTaskId);
            SetNextState();
            return;
        }
        
        Vector3 flatTargetPos = Target.CellPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        if (distance > TotalAttackRange) return;
        
        if (_strongAttack)
        {
            Room.SpawnProjectile(ProjectileId.SunfloraPixieFire, this, 5f);
        }
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            if (_strongAttack == false)
            {
                Room.SpawnProjectile(ProjectileId.SunfloraPixieProjectile, this, 5f);
            }
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || AddBuffAction == null) return;

            AttackEnded = true;
            var types = new[] { GameObjectType.Tower };
            var num = _triple ? 3 : 2;
            var skillTargets = Room.FindTargets(this, types, TotalSkillRange, AttackType);
            
            // Recover Mp
            if (_recoverMp)
            {
                var mpTargets = skillTargets
                    .Where(target => 
                        target.MaxMp != 1 && target is Creature creature 
                                          && creature.UnitId != UnitId.SunBlossom 
                                          && creature.UnitId != UnitId.SunflowerFairy 
                                          && creature.UnitId != UnitId.SunfloraPixie)
                    .OrderBy(_ => Guid.NewGuid()).Take(num).ToList();
                foreach (var target in mpTargets)
                {
                    target.Mp += _mpRecoverParam;
                }
            }
            
            // Invincible - Instead shield
            if (_invincible)
            {
                var targets = Room.FindTargets(this, types, TotalSkillRange, AttackType)
                    .OrderByDescending(target => Way == SpawnWay.North ? target.CellPos.Z : -target.CellPos.Z)
                    .Take(num).ToList();
                foreach (var target in targets)
                {
                    Room.Push(AddBuffAction, BuffId.Invincible,
                        BuffParamType.None, target, this, 0, 3000, false);
                }
            }
        });
    }

    public override void ApplyProjectileEffect(GameObject target, ProjectileId projectileId)
    {
        if (projectileId == ProjectileId.SunfloraPixieProjectile)
        {
            Room?.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        }
        else
        {
            Room?.Push(target.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
        }
    }
}
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
                    Attack += 40;
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
        
        Player.SkillSubject.SkillUpgraded(Skill.SunfloraPixieRecoverMp);
        Player.SkillSubject.SkillUpgraded(Skill.SunfloraPixieStrongAttack);
        Player.SkillSubject.SkillUpgraded(Skill.SunfloraPixieInvincible);
        Player.SkillSubject.SkillUpgraded(Skill.SunfloraPixieDebuffRemove);
        Player.SkillSubject.SkillUpgraded(Skill.SunfloraPixieTripleBuff);
    }
    
    protected override void UpdateAttack()
    {
        if (Room == null) return;
        
        Room.SpawnProjectile(_strongAttack ? 
            ProjectileId.SunfloraPixieFire : ProjectileId.SunfloraPixieProjectile, this, 5f);
        
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            IsAttacking = false;
            Scheduler.CancelEvent(AttackTaskId);
            return;
        }
        
        if (IsAttacking) return;

        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactMoment = (long)(StdAnimTime / TotalAttackSpeed * AttackImpactMoment);
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        long impactMomentCorrection = Math.Max(0, LastAnimEndTime - timeNow + impactMoment);
        long animPlayTimeCorrection = Math.Max(0, LastAnimEndTime - timeNow + animPlayTime);
        long impactTime = AttackEnded ? impactMoment : Math.Min(impactMomentCorrection, impactMoment);
        long animEndTime = AttackEnded ? animPlayTime : Math.Min(animPlayTimeCorrection, animPlayTime);
        AttackImpactEvents(impactTime);
        EndEvents(animEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        AttackEnded = false;
        IsAttacking = true;
    }   
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.Push(Target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || AddBuffAction == null) return;
            
            var types = new[] { GameObjectType.Tower };
            var num = _triple ? 3 : 2;
            
            // Heal
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
            
            // Recover Mp
            if (_recoverMp)
            {
                var mpTargets = skillTargets
                    .Where(target => target.MaxMp != 1)
                    .OrderBy(_ => Guid.NewGuid()).Take(num).ToList();
                foreach (var target in mpTargets)
                {
                    target.Mp += _mpRecoverParam;
                }
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
            
            Mp = 0;
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId projectileId) { }
}
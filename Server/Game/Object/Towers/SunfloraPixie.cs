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
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(_strongAttack ? 
                ProjectileId.SunfloraPixieFire : ProjectileId.SunfloraPixieProjectile, this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            
            var types = new[] { GameObjectType.Tower };
            var num = _triple ? 3 : 2;
            
            // Heal
            var skillTargets = Room.FindTargets(this, types, TotalSkillRange, AttackType);
            var healTargets = skillTargets
                .OrderBy(target => target.Hp / target.MaxHp).Take(num).ToList();
            foreach (var target in healTargets)
            {
                BuffManager.Instance.AddBuff(BuffId.HealBuff, BuffParamType.Constant,
                    target, this, HealParam);
                // Debuff Remove
                if (_debuffRemove && target is Creature creature) BuffManager.Instance.RemoveAllDebuff(creature);
            }
            
            // Recover Mp
            if (_recoverMp)
            {
                var mpTargets = skillTargets.OrderBy(_ => Guid.NewGuid()).Take(num).ToList();
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
                BuffManager.Instance.AddBuff(BuffId.HealBuff, BuffParamType.Constant,
                    target, this, FenceHealParam);
            }
            
            // Invincible - Instead shield
            if (_invincible)
            {
                var targets = Room.FindTargets(this, types, TotalSkillRange, AttackType)
                    .OrderByDescending(target => target.CellPos.Z).Take(num).ToList();
                foreach (var target in targets)
                {
                    BuffManager.Instance.AddBuff(BuffId.Invincible, BuffParamType.None,
                        target, this, 0, 3000);
                }
            }
            
            Mp = 0;
        });
    }
}
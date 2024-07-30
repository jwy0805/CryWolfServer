using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class SunflowerFairy : SunBlossom
{
    private bool _fenceHeal;
    private bool _shield;
    private bool _double;
    
    protected readonly int FenceHealParam = 100;
    protected readonly int ShieldParam = 180;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SunflowerFairyFenceHeal:
                    _fenceHeal = true;
                    break;
                case Skill.SunflowerFairyShield:
                    _shield = true;
                    break;
                case Skill.SunflowerFairyHealParamUp:
                    HealParam = 120;
                    break;
                case Skill.SunflowerFairyDoubleBuff:
                    _double = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Supporter;
    }
    
    protected override void UpdateIdle()
    {   // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 flatTargetPos = Target.CellPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);

        if (distance > TotalAttackRange) return;
        State = Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;        
            Room.SpawnProjectile(ProjectileId.SunfloraPixieProjectile, this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || AddBuffAction == null) return;
            
            var types = new[] { GameObjectType.Tower };
            
            // Heal
            var healTargets = Room.FindTargets(this, types, TotalSkillRange, AttackType)
                .OrderBy(target => target.Hp / target.MaxHp)
                .Take(_double ? 2 : 1)
                .ToList();
            foreach (var target in healTargets)
            {
                Room.Push(AddBuffAction, BuffId.HealBuff, 
                    BuffParamType.Constant, target, this, HealParam, 1000, false);
            }
            
            // Fence Heal
            if (_fenceHeal)
            {
                var fenceType = new[] { GameObjectType.Fence };
                var targets = Room.FindTargets(this, fenceType, TotalSkillRange, AttackType)
                    .OrderBy(target => target.Hp)
                    .Take(_double ? 2 : 1)
                    .ToList();
                foreach (var target in targets)  
                {
                    Room.Push(AddBuffAction, BuffId.HealBuff,
                        BuffParamType.Constant, target, this, FenceHealParam, 1000, false);
                }
            }
            
            // Shield - Instead defence buff
            if (_shield)
            {
                var targets = Room.FindTargets(this, types, TotalSkillRange, AttackType)
                    .OrderByDescending(target => Way == SpawnWay.North ? target.CellPos.Z : -target.CellPos.Z)
                    .Take(_double ? 2 : 1)
                    .ToList();
                foreach (var target in targets)
                {
                    target.ShieldAdd = ShieldParam;
                }
            }
            
            Mp = 0;
        });
    }
}
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Skeleton : Monster
{
    private bool _defenceDown;
    private bool _nestedDebuff;
    private bool _additionalDamage;

    protected int AdditionalAttackParam;
    protected int PreviousTargetId;
    protected int DefenceDownParam;
    
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SkeletonDefenceDown:
                    _defenceDown = true;
                    DefenceDownParam = 7;
                    break;
                case Skill.SkeletonNestedDebuff:
                    _nestedDebuff = true;
                    DefenceDownParam = 3;
                    break;
                case Skill.SkeletonAdditionalDamage:
                    _additionalDamage = true;
                    break;
                case Skill.SkeletonAttackSpeed:
                    AttackSpeedParam += AttackSpeed * 0.15f;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Supporter;
        Player.SkillSubject.SkillUpgraded(Skill.SkeletonDefenceDown);
        Player.SkillSubject.SkillUpgraded(Skill.SkeletonNestedDebuff);
        Player.SkillSubject.SkillUpgraded(Skill.SkeletonAdditionalDamage);
        Player.SkillSubject.SkillUpgraded(Skill.SkeletonAttackSpeed);
    }
    
    protected override void UpdateMoving()
    {   // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        if (distance <= TotalAttackRange)
        {
            State = GetRandomState(State.Attack, State.Attack2);
            SyncPosAndDir();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    protected override void UpdateAttack2()
    {
        UpdateAttack();
    }
    
    public override void ApplyAttackEffect(GameObject target)
    {
        if (Room == null || AddBuffAction == null) return;
        
        var effectPos = new PositionInfo
        {
            PosX = target.CellPos.X, PosY = target.CellPos.Y + 0.5f, PosZ = target.CellPos.Z
        };
        var duration = (int)(1000 / TotalAttackSpeed);

        if (PreviousTargetId != target.Id)
        {
            AdditionalAttackParam = 0;
            PreviousTargetId = target.Id;
        }

        if (_defenceDown)
        {
            if (_nestedDebuff)
            {
                target.DefenceParam -= DefenceDownParam;
            }
            else
            {
                Room.Push(AddBuffAction, BuffId.DefenceDebuff, 
                    BuffParamType.Constant, target, this, DefenceDownParam, 5000, false);
            }
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        if (target.Hp <= 0) return;
        
        if (_additionalDamage)
        {
            if (target.TotalDefence <= 0) AdditionalAttackParam += DefenceDownParam;
            
            if (AdditionalAttackParam > 0)
            {
                Room.Push(target.OnDamaged, this, AdditionalAttackParam, Damage.Magical, false);
                if (target.Hp <= 0) return;
            }
            
            Room.SpawnEffect(EffectId.SkeletonAdditionalEffect, this, target, effectPos, true, duration);
        }
        else
        {
            Room.SpawnEffect(EffectId.SkeletonEffect, this, target, effectPos, true, duration);
        }
    }

    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0 || Target.Room == null)
        {
            State = State.Idle;
            AttackEnded = true;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);  

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
            AttackEnded = true;
        }
        else
        {
            State = GetRandomState(State.Attack, State.Attack2);
            SyncPosAndDir();
        }
    }
}
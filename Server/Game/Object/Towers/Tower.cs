using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Tower : Creature, ISkillObserver
{
    public UnitId UnitId { get; set; }
    public Vector3 StartCell { get; set; }

    protected Tower()
    {
        ObjectType = GameObjectType.Tower;
    }

    public override void Init()
    {
        DataManager.UnitDict.TryGetValue((int)UnitId, out var unitData);
        Stat.MergeFrom(unitData?.stat);
        base.Init();
        
        StatInit();
        Player.SkillSubject.AddObserver(this);
        State = State.Idle;
    }

    protected override void UpdateIdle()
    {   // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        float distance = Vector3.Distance(Target.CellPos, CellPos);
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        if (distance > AttackRange) return;
        State = State.Attack;
    }

    protected override void UpdateAttack()
    {
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            IsAttacking = false;
            return;
        }
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (IsAttacking) return;
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactTime = (long)(StdAnimTime / TotalAttackSpeed * AttackImpactTime);
        long animTime = (long)(StdAnimTime / TotalAttackSpeed);
        long nextAnimEndTime = StateChanged ? animTime : LastAttackTime - timeNow + animTime;
        long nextImpactTime = StateChanged ? impactTime : LastAttackTime - timeNow + impactTime;
        AttackImpactEvents(nextImpactTime);
        EndEvents(nextAnimEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        IsAttacking = true;
    }

    public override void OnDead(GameObject attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        base.OnDead(attacker);
    }

    public virtual void OnSkillUpgrade(Skill skill)
    {
        var skillName = skill.ToString();
        var towerName = UnitId.ToString();
        if (skillName.Contains(towerName))
        {
            NewSkill = skill;
            SkillList.Add(NewSkill);
        }
    }
    
    public override void SkillInit()
    {
        var skillUpgradedList = Player.SkillUpgradedList;
        var towerName = UnitId.ToString();
        if (skillUpgradedList.Count == 0) return;
        
        foreach (var skill in skillUpgradedList)
        {
            string skillName = skill.ToString();
            if (skillName.Contains(towerName)) SkillList.Add(skill);
        }

        if (SkillList.Count != 0)
        {
            foreach (var skill in SkillList) NewSkill = skill;
        }
    }
}
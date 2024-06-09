using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Monster : Creature, ISkillObserver
{
    public UnitId UnitId { get; set; }
    public int StatueId { get; set; }

    protected Monster()
    {
        ObjectType = GameObjectType.Monster;
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
    {
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        State = State.Moving;
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
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        // Target이 사정거리 안에 있다가 밖으로 나간 경우 애니메이션 시간 고려하여 Attack 상태로 변경되도록 조정
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        if (distance <= TotalAttackRange && timeNow < LastAnimEndTime + animPlayTime)
        {
            State = State.Attack;
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }


    
    public override void OnDead(GameObject attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        base.OnDead(attacker);
    }

    public virtual void OnSkillUpgrade(Skill skill)
    {
        var skillName = skill.ToString();
        var monsterName = UnitId.ToString();
        if (skillName.Contains(monsterName) == false) return;
        NewSkill = skill;
        SkillList.Add(NewSkill);
    }

    public override void SkillInit()
    {
        var skillUpgradedList = Player.SkillUpgradedList;
        var monsterName = UnitId.ToString();
        if (skillUpgradedList.Count == 0) return;
        
        foreach (var skill in skillUpgradedList)
        {
            var skillName = skill.ToString();
            if (skillName.Contains(monsterName)) SkillList.Add(skill);
        }
        
        if (SkillList.Count == 0) return;
        foreach (var skill in SkillList) NewSkill = skill;
    }
}
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
        base.Init();
        DataManager.UnitDict.TryGetValue((int)UnitId, out var unitData);
        Stat.MergeFrom(unitData?.stat);
        StatInit();
        
        Player.SkillSubject.AddObserver(this);
        SearchTick = 250;
        LastSearch = 0;
        State = State.Idle;
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this);
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        if (Target == null) return;

        var targetStat = Target.Stat;
        if (targetStat.Targetable == false) return;
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        if (distance > AttackRange) return;
        State = State.Attack;
        BroadcastPos();
    }

    protected override void UpdateAttack()
    {
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastPos();
        }
    }

    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }

        if (Target.Hp <= 0)
        {
            Target = null;
            State = State.Idle;
            BroadcastPos();
            return;
        }

        var distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
        if (distance > TotalAttackRange) return;
        SetDirection();
        State = State.Attack;
        BroadcastPos();
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
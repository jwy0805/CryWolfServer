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
        Target = Room?.FindClosestTarget(this);
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        if (Target == null) return;
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        
        (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
        BroadcastDest();
        
        State = State.Moving;
        BroadcastMove();
    }

    protected override void UpdateMoving()
    {   
        // Targeting
        Target = Room?.FindClosestTarget(this);
        if (Target != null)
        {
            DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
            (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos, false);
            BroadcastDest();
        }
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }

        if (Room != null)
        {
            // 이동
            // target이랑 너무 가까운 경우
            // Attack
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                if (distance <= AttackRange)
                {
                    CellPos = position;
                    State = State.Attack;
                    BroadcastMove();
                    return;
                }
            }
            
            BroadcastMove();
        }
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
        if (skillName.Contains(monsterName))
        {
            NewSkill = skill;
            SkillList.Add(NewSkill);
        }
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

        if (SkillList.Count != 0)
        {
            foreach (var skill in SkillList) NewSkill = skill;
        }
    }
}
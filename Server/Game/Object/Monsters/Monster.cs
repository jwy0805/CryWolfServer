using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Monster : Creature, ISkillObserver
{
    public int MonsterNum;
    public MonsterId MonsterId;

    protected Monster()
    {
        ObjectType = GameObjectType.Monster;
    }

    public override void Init()
    {
        DataManager.MonsterDict.TryGetValue(MonsterNum, out var monsterData);
        Stat.MergeFrom(monsterData!.stat);
        base.Init();
        SkillInit(); 
        Hp = MaxHp;

        Player.SkillSubject.AddObserver(this);
        State = State.Idle;
    }

    protected override void UpdateIdle()
    {
        GameObject? target = Room?.FindNearestTarget(this);
        if (target == null || Room == null) return;
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        Target = target;
        DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
        // Console.WriteLine($"{DestPos.X}, {DestPos.Z}");
        (Path, Atan) = Room.Map.Move(this, CellPos, DestPos);
        BroadcastDest();
        State = State.Moving;
    }

    protected override void UpdateMoving()
    {
        // Targeting
        double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
        if (timeNow > LastSearch + SearchTick)
        {
            LastSearch = timeNow;
            GameObject? target = Room?.FindNearestTarget(this);
            if (Target?.Id != target?.Id)
            {
                Target = target;
                if (Target != null)
                {
                    DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                    (Path, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                }
            }
        }
        
        if (Target == null || Target.Room != Room)
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
            
            // Console.WriteLine($"CP: {CellPos.X}, {CellPos.Z} / DP: {DestPos.X}, {DestPos.Z} / Dir: {Dir} / Target: {Target.CellPos.X}, {Target.CellPos.Z}");
            BroadcastMove();
        }
    }

    protected override void UpdateAttack()
    {
        if (Room == null) return;
        if (Target == null)
        {
            State = State.Idle;
            return;
        }
        if (Target.Stat.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            return;
        }

        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        BroadcastMove();
    }

    public override void OnDead(GameObject attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        base.OnDead(attacker);
    }

    public virtual void OnSkillUpgrade(Skill skill)
    {
        string skillName = skill.ToString();
        string monsterName = MonsterId.ToString();
        if (skillName.Contains(monsterName))
        {
            NewSkill = skill;
            SkillList.Add(NewSkill);
        }
    }

    protected override void SkillInit()
    {
        List<Skill> skillUpgradedList = Player.SkillUpgradedList;
        string monsterName = MonsterId.ToString();
        if (skillUpgradedList.Count == 0) return;
        
        foreach (var skill in skillUpgradedList)
        {
            string skillName = skill.ToString();
            if (skillName.Contains(monsterName)) SkillList.Add(skill);
        }

        if (SkillList.Count != 0)
        {
            foreach (var skill in SkillList) NewSkill = skill;
        }
    }
}
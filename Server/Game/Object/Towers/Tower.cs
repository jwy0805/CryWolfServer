using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Tower : Creature, ISkillObserver
{
    public int TowerNum;
    public TowerId TowerId;
    
    public Tower()
    {
        ObjectType = GameObjectType.Tower;
    }

    public override void Init()
    {
        DataManager.TowerDict.TryGetValue(TowerNum, out var towerData);
        Stat.MergeFrom(towerData!.stat);
        base.Init();
        SkillInit();
        Hp = MaxHp;
        
        Player.SkillSubject.AddObserver(this);
        SearchTick = 250;
        LastSearch = 0;
        State = State.Idle;
    }

    protected override void UpdateIdle()
    {
        GameObject? target = Room?.FindNearestTarget(this);
        if (target == null) return;
        LastSearch = Room!.Stopwatch.ElapsedMilliseconds;
        Target ??= target;

        StatInfo targetStat = Target.Stat;
        if (targetStat.Targetable)
        {
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
            double deltaX = Target.CellPos.X - CellPos.X;
            double deltaZ = Target.CellPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= AttackRange)
            {
                State = State.Attack;
                BroadcastMove();
            }
        }
    }

    protected override void UpdateAttack()
    {
        if (Room == null) return;
        if (Target == null)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }

        if (Target.Stat.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        BroadcastMove();
    }

    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Stat.Targetable == false)
        {
            State = State.Idle;
        }
        else
        {
            if (Target.Hp > 0)
            {
                Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));
                if (distance <= AttackRange)
                {
                    State = State.Attack;
                }
                else
                {
                    State = State.Idle;
                }
            }
            else
            {
                Target = null;
                State = State.Idle;
            }
        }
        
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }

    public override void OnDead(GameObject attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        base.OnDead(attacker);
    }

    public virtual void OnSkillUpgrade(Skill skill)
    {
        string skillName = skill.ToString();
        string towerName = TowerId.ToString();
        if (skillName.Contains(towerName))
        {
            NewSkill = skill;
            SkillList.Add(NewSkill);
        }
    }
    
    protected override void SkillInit()
    {
        List<Skill> skillUpgradedList = Player.SkillUpgradedList;
        string towerName = TowerId.ToString();
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
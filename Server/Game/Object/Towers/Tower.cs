using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Tower : Creature, ISkillObserver
{
    public int TowerNum { get; set; }
    public TowerId TowerId { get; set; }
    public Vector3 StartCell { get; set; }

    protected Tower()
    {
        ObjectType = GameObjectType.Tower;
    }

    public override void Init()
    {
        DataManager.TowerDict.TryGetValue(TowerNum, out var towerData);
        Behavior = (Behavior)Enum.Parse(typeof(Behavior), towerData!.behavior);
        Stat.MergeFrom(towerData?.stat);
        base.Init();
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
        Target ??= target;
        if (Target == null) return;

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
        if (Target == null || Target.Stat.Targetable == false)
        {
            State = State.Idle;
            BroadcastMove();
        }
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
            if (Target.Hp > 0 && Target != null)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
                if (distance <= AttackRange)
                {
                    State = State.Attack;
                    SetDirection();
                }
                else
                {
                    State = State.Idle;
                }
            }
            else
            {
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
    
    public override void SkillInit()
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
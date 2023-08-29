using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Tower : Creature, ISkillObserver
{
    public int TowerNum;
    public TowerId TowerId;
    
    public Tower()
    {
        ObjectType = GameObjectType.Tower;
    }

    public void Init(int towerNo)
    {
        TowerNum = towerNo;

        DataManager.TowerDict.TryGetValue(TowerNum, out var towerData);
        Stat.MergeFrom(towerData!.stat);
        Stat.Hp = towerData.stat.MaxHp;
        
        Player.SkillSubject.AddObserver(this);
        State = State.Idle;
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
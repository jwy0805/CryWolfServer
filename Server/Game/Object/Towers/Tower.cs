using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Tower : Creature, ISkillObserver
{
    public int TowerNo;
    public TowerId TowerId;
    
    public Tower()
    {
        ObjectType = GameObjectType.Tower;
    }

    public void Init(int towerNo)
    {
        TowerNo = towerNo;

        DataManager.TowerDict.TryGetValue(TowerNo, out var towerData);
        Stat.MergeFrom(towerData!.stat);
        Stat.Hp = towerData.stat.MaxHp;

        State = State.Idle;
    }

    public void OnSkillUpgrade(Skill skill)
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
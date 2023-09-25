using Google.Protobuf.Protocol;

namespace Server.Game;

public class SunflowerFairy : SunBlossom
{
    protected float AttackParam = 0.1f;
    protected float DefenceParam = 5;
    
    private bool _attackBuff = false;
    private bool _defenceBuff = false;
    private bool _double = false;
    private bool _fenceHeal = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SunflowerFairyAttack:
                    _attackBuff = true;
                    break;
                case Skill.SunflowerFairyDefence:
                    _defenceBuff = true;
                    break;
                case Skill.SunflowerFairyDouble:
                    _double = true;
                    break;
                case Skill.SunflowerFairyMpDown:
                    MaxMp = 45;
                    break;
                case Skill.SunflowerFairyFenceHeal:
                    _fenceHeal = true;
                    break;
            }
        }
    }
    
    public override void RunSkill()
    {
        if (Room == null) return;
        
        List<GameObject> towers = new List<GameObject>();
        List<GameObject> monsters = new List<GameObject>();
        if (_double)
        {
            towers = Room.FindBuffTargets(this, GameObjectType.Tower, 2);
            monsters = Room.FindBuffTargets(this, GameObjectType.Monster, 2);
        }
        else
        {
            GameObject? tower = Room.FindBuffTarget(this, GameObjectType.Tower);
            GameObject? monster = Room.FindBuffTarget(this, GameObjectType.Monster);
            if (tower != null) towers.Add(tower);
            if (monster != null) monsters.Add(monster);
        }

        if (towers.Count != 0)
        {
            foreach (var tower in towers)
            {
                tower.Hp += HealParam;
                BuffManager.Instance.AddBuff(BuffId.HealthIncrease, tower, HealthParam);
                if (_attackBuff) BuffManager.Instance.AddBuff(BuffId.AttackIncrease, tower, AttackParam);
                if (_defenceBuff) BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, tower, DefenceParam);
            }
        }

        if (monsters.Count != 0)
        {
            foreach (var monster in monsters)
            {
                BuffManager.Instance.AddBuff(BuffId.MoveSpeedDecrease, monster, SlowParam);
                BuffManager.Instance.AddBuff(BuffId.AttackSpeedDecrease, monster, SlowAttackParam);
            }
        }
    }
}
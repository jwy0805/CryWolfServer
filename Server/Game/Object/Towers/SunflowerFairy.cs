using Google.Protobuf.Protocol;

namespace Server.Game;

public class SunflowerFairy : SunBlossom
{
    protected readonly float AttackParam = 0.1f;
    protected readonly int DefenceParam = 5;
    protected int FenceHealParam = 90;
    
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
        
        List<Creature> towers = Room.FindTargets(this, 
            new List<GameObjectType> { GameObjectType.Tower }, SkillRange).Cast<Creature>().ToList();
        if (towers.Count != 0)
        {
            foreach (var tower in towers)
            {
                tower.Hp += HealParam;
                Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
                BuffManager.Instance.AddBuff(BuffId.HealthIncrease, tower, this, HealthParam);
                if (_attackBuff) BuffManager.Instance.AddBuff(BuffId.AttackIncrease, tower, this, AttackParam);
                if (_defenceBuff) BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, tower, this, DefenceParam);
            }
        }

        int num = _double ? 2 : 1;
        List<Creature> monsters = Room.FindTargets(this,
            new List<GameObjectType> { GameObjectType.Monster }, SkillRange).Cast<Creature>().ToList();
        if (monsters.Any())
        {
            foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())   
                BuffManager.Instance.AddBuff(BuffId.MoveSpeedDecrease, monster, this, SlowParam);
            foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())
                BuffManager.Instance.AddBuff(BuffId.AttackSpeedDecrease, monster, this, SlowAttackParam);
        }
        
        if (_fenceHeal)
        {
            List<GameObject> fences = Room.FindTargets(this, 
                new List<GameObjectType> { GameObjectType.Fence }, SkillRange);
            if (fences.Any())
            {
                foreach (var fence in fences)
                {
                    fence.Hp += FenceHealParam;
                    Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
                }
            }
        }
    }
}
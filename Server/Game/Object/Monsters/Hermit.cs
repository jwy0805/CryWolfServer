using Google.Protobuf.Protocol;

namespace Server.Game;

public class Hermit : Spike
{
    private bool _debuffRemove = false;
    private bool _aggro = false;
    private bool _faint = false;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            // switch (Skill)
            // {
            //     case Skill.HermitPoisonResist:
            //         PoisonResist += 25;
            //         break;
            //     case Skill.HermitFireResist:
            //         FireResist += 25;
            //         break;
            //     case Skill.HermitDebuffRemove:
            //         _debuffRemove = true;
            //         break;
            //     case Skill.HermitRange:
            //         //어그로 범위 증가
            //         AttackRange += 2.0f;
            //         break;
            //     case Skill.HermitAggro:
            //         _aggro = true;
            //         break;
            //     case Skill.HermitReflection:
            //         Reflection = true;
            //         break;
            //     case Skill.HermitFaint:
            //         _faint = true;
            //         break;
            // }
        }
    }

    public override void RunSkill()
    {
        if (Room == null) return;
        
        List<Creature> monsters = Room.FindTargets(this, 
            new List<GameObjectType> { GameObjectType.Monster }, SkillRange).Cast<Creature>().ToList();
        
        foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(2).ToList())
            BuffManager.Instance.AddBuff(BuffId.MoveSpeedIncrease, monster, this, MoveSpeedParam);
        foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(2).ToList())
            BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, monster, this, AttackSpeedParam);
        foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(2).ToList())
            BuffManager.Instance.AddBuff(BuffId.AttackIncrease, monster, this, AttackBuffParam);
        foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(2).ToList())
            BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, monster, this, DefenceBuffParam);

        if (_debuffRemove)
        {
            foreach (var monster in monsters)
                BuffManager.Instance.RemoveAllDebuff(monster);
        }
        
        if (_aggro)
        {
            foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(2).ToList())
                BuffManager.Instance.AddBuff(BuffId.Aggro, monster, this, 0, 3000);
        }
    }
}
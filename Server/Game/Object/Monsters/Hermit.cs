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
            switch (Skill)
            {
                case Skill.HermitPoisonResist:
                    PoisonResist += 25;
                    break;
                case Skill.HermitFireResist:
                    FireResist += 25;
                    break;
                case Skill.HermitDebuffRemove:
                    _debuffRemove = true;
                    break;
                case Skill.HermitRange:
                    //어그로 범위 증가
                    AttackRange += 2.0f;
                    break;
                case Skill.HermitAggro:
                    _aggro = true;
                    break;
                case Skill.HermitReflection:
                    Reflection = true;
                    break;
                case Skill.HermitFaint:
                    _faint = true;
                    break;
            }
        }
    }
        
    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.Hermit;
    }

    public override void RunSkill()
    {
        List<GameObject?> gameObjects = new List<GameObject?>();
        gameObjects = Room?.FindBuffTargets(this, GameObjectType.Monster, 2)!;

        if (gameObjects.Count == 0) return;
        foreach (var gameObject in gameObjects)
        {
            Creature creature = (Creature)gameObject!;
            BuffManager.Instance.AddBuff(BuffId.MoveSpeedIncrease, creature, MoveSpeedParam);
            BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, creature, AttackSpeedParam);
            BuffManager.Instance.AddBuff(BuffId.AttackIncrease, creature, AttackBuffParam);
            BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, creature, DefenceBuffParam);
        }
    }
}
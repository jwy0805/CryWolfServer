using Google.Protobuf.Protocol;

namespace Server.Game;

public class Spike : Shell
{
    private bool _lostHeal = false;
    private bool _attackBuff = false;
    private bool _defenceBuff = false;
    private bool _doubleBuff = false;
    
    protected readonly float AttackBuffParam = 2.0f;
    protected readonly int DefenceBuffParam = 6;
    protected readonly float LostHealParam = 0.3f;
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SpikeSelfDefence:
                    Defence += 10;
                    break;
                case Skill.SpikeLostHeal:
                    _lostHeal = true;
                    break;
                case Skill.SpikeAttack:
                    _attackBuff = true;
                    break;
                case Skill.SpikeDefence:
                    _defenceBuff = true;
                    break;
                case Skill.SpikeDoubleBuff:
                    _doubleBuff = true;
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.Spike;
    }

    public override void RunSkill()
    {
        List<GameObject?> gameObjects = new List<GameObject?>();
        if (_doubleBuff)
            gameObjects = Room?.FindBuffTargets(this, GameObjectType.Monster, 2)!;
        else gameObjects.Add(Room?.FindBuffTarget(this, GameObjectType.Monster));

        if (gameObjects.Count == 0) return;
        foreach (var gameObject in gameObjects)
        {
            Creature creature = (Creature)gameObject!;
            BuffManager.Instance.AddBuff(BuffId.MoveSpeedIncrease, creature, MoveSpeedParam);
            BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, creature, AttackSpeedParam);
            if (_attackBuff) BuffManager.Instance.AddBuff(BuffId.AttackIncrease, creature, AttackBuffParam);
            if (_defenceBuff) BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, creature, DefenceBuffParam);   
        }
    }
}
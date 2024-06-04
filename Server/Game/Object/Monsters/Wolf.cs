using Google.Protobuf.Protocol;

namespace Server.Game;

public class Wolf : WolfPup
{
    private bool _drain = false;
    protected readonly float DrainParam = 0.18f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.WolfHealth:
                    MaxHp += 80;
                    Hp += 80;
                    BroadcastHealth();
                    break;
                case Skill.WolfMoreDna:
                    break;
                case Skill.WolfDrain:
                    _drain = true;
                    break;
                case Skill.WolfCritical:
                    CriticalChance += 20;
                    break;
                case Skill.WolfLastHitDna:
                    break;
            }
        }
    }

    public override void ApplyNormalAttackEffect(GameObject target)
    {
        base.ApplyNormalAttackEffect(target);
        if (_drain)
        {
            Hp += (int)((TotalAttack - target.TotalDefence) * DrainParam);
            BroadcastHealth();
        }
        
        // TODO : DNA
    }
}
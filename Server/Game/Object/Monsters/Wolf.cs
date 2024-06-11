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
                    BroadcastHp();
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

    public override void ApplyAttackEffect(GameObject target)
    {
        if (Room == null || Hp <= 0) return;
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        
        if (_drain)
        {
            Hp += (int)((TotalAttack - target.TotalDefence) * DrainParam);
            BroadcastHp();
        }
        
        // TODO : DNA
    }
}
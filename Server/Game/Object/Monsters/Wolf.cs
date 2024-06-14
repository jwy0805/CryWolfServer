using Google.Protobuf.Protocol;

namespace Server.Game;

public class Wolf : WolfPup
{
    private bool _drain = false;
    protected float DrainParam = 0.12f;
    
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
        if (_drain)
        {
            var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
            Hp += (int)(damage * DrainParam);
            BroadcastHp();
        }
        
        target.OnDamaged(this, TotalAttack, Damage.Normal);

        // TODO : DNA
    }
}
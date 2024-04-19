using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoPester : MosquitoBug
{
    private bool _poison = false;
    private bool _woolDown = false;
    
    protected int WoolDownRate = 20;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoPesterPoison:
                    _poison = true;
                    break;
                case Skill.MosquitoPesterWoolRate:
                    _woolDown = true;
                    break;
                case Skill.MosquitoPesterPoisonResist:
                    PoisonResist += 20;
                    break;
                case Skill.MosquitoPesterEvasion:
                    Evasion += 15;
                    break;
                case Skill.MosquitoPesterHealth:
                    MaxHp += 30;
                    Hp += 30;
                    BroadcastHealth();
                    break;
            }
        }
    }

    public override void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);

        if (target is Creature _)
        {
            BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1);

            if (_poison)
            {
                BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0, 5);
            }
        }

        if (target is not Sheep sheep) return;
        if (_woolDown) sheep.YieldDecrement = sheep.Resource * WoolDownRate / 100;
        BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1);
    }
}
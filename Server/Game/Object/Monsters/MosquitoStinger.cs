using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoStinger : MosquitoPester
{
    private bool _longAttack = false;
    private bool _poison = false;
    private bool _sheepDeath = false;
    private bool _infection = false;
    private float _deathRate = 0;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoStingerAvoid:
                    Evasion += 15;
                    break;
                case Skill.MosquitoStingerHealth:
                    MaxHp += 60;
                    Hp += 60;
                    break;
                case Skill.MosquitoStingerLongAttack:
                    _longAttack = true;
                    AttackRange += 3;
                    break;
                case Skill.MosquitoStingerPoison:
                    _poison = true;
                    break;
                case Skill.MosquitoStingerPoisonResist:
                    PoisonResist += 20;
                    break;
                case Skill.MosquitoStingerInfection:
                    _infection = true;
                    break;
                case Skill.MosquitoStingerSheepDeath:
                    _sheepDeath = true;
                    break;
            }
        }
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        
    }

    public override void RunSkill()
    {
        
    }
}
using Google.Protobuf.Protocol;

namespace Server.Game;

public class Cactus : Cacti
{
    private bool _reflectionFaint = false;
    
    protected readonly int ReflectionFaintRate = 3;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CactusSpeed:
                    MoveSpeed += 1;
                    break;
                case Skill.CactusPoisonResist:
                    PoisonResist += 15;
                    break;
                case Skill.CactusReflection:
                    Reflection = true;
                    ReflectionRate = 15;
                    break;
                case Skill.CactusReflectionFaint:
                    _reflectionFaint = true;
                    break;
            }
        }
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (_reflectionFaint)
        {
            if (Room == null) return;
            if (Invincible) return;

            int totalDamage;
            if (damageType is Damage.Normal or Damage.Magical)
            {
                totalDamage = attacker.CriticalChance > 0 
                    ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
                    : Math.Max(damage - TotalDefence, 0);
                if (damageType is Damage.Normal && Reflection && reflected == false)
                {
                    int refParam = (int)(totalDamage * ReflectionRate / 100);
                    attacker.OnDamaged(this, refParam, damageType, true);
                    var random = new Random();
                    if (random.Next(99) >= ReflectionFaintRate) return;
                    BuffManager.Instance.AddBuff(BuffId.Fainted, attacker, this, 0, 1000);
                }
            }
            else
            {
                totalDamage = damage;
            }
        
            Hp = Math.Max(Hp - totalDamage, 0);
            var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
            Room.Broadcast(damagePacket);
            if (Hp <= 0) OnDead(attacker);
        }
        else
        {
            base.OnDamaged(attacker, damage, damageType, reflected);
        }
    }
}
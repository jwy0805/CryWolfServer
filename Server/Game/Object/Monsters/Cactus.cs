using Google.Protobuf.Protocol;

namespace Server.Game;

public class Cactus : Cacti
{
    private bool _reflectionFaint = false;
    protected readonly int ReflectionFaintRate = 5;
    
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
                    ReflectionRate = 10;
                    break;
                case Skill.CactusReflectionFaint:
                    _reflectionFaint = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
    }

    public override void OnDamaged(GameObject? attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        if (new Random().Next(100) < TotalEvasion)
        {
            // TODO: Evasion Effect
            return;
        }

        var totalDamage = damageType is Damage.Normal or Damage.Magical 
            ? Math.Max(damage - TotalDefence, 0) : damage;
        if (damageType is Damage.Normal && Reflection && reflected == false)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            attacker?.OnDamaged(this, reflectionDamage, damageType, true);
            if (_reflectionFaint && new Random().Next(99) < ReflectionFaintRate && attacker != null)
            {
                BuffManager.Instance.AddBuff(BuffId.Fainted, attacker, this, 0, 1000);
            }
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp <= 0) OnDead(attacker);
    }
}
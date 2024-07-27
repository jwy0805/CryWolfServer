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
        UnitRole = Role.Tanker;
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null || AddBuffAction == null) return;
        if (Invincible) return;
        var random = new Random();
        var totalDamage = damageType is Damage.Normal or Damage.Magical 
            ? Math.Max(damage - TotalDefence, 0) : damage;
        
        if (random.Next(100) < attacker.CriticalChance)
        {
            totalDamage = (int)(totalDamage * attacker.CriticalMultiplier);
        }
        
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {
            // TODO: Evasion Effect
            return;
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        
        if (Hp <= 0)
        {
            OnDead(attacker);
            return;
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            Room.Push(attacker.OnDamaged, this, reflectionDamage, damageType, true);
            
            if (_reflectionFaint && new Random().Next(100) < ReflectionFaintRate && attacker.Targetable)
            {
                Room.Push(AddBuffAction, BuffId.Fainted,
                    BuffParamType.None, attacker, this, 0, 1000, false);
            }
        }
    }
}
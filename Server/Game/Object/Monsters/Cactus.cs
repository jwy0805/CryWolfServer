using Google.Protobuf.Protocol;
using Server.Data;
using Random = System.Random;

namespace Server.Game;

public class Cactus : Cacti
{
    private bool _reflectionFaint = false;
    protected readonly int ReflectionFaintRate = (int)DataManager.SkillDict[(int)Skill.CactusReflectionFaint].Value;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CactusSpeed:
                    MoveSpeed += DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.CactusFireResist:
                    FireResist += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.CactusReflection:
                    Reflection = true;
                    ReflectionRate = DataManager.SkillDict[(int)Skill].Value;
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
    
    public override void ApplyAttackEffect(GameObject target)
    {
        Room?.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        Room?.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.MonsterAttack, SoundType = SoundType.D3 });
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null || AddBuffAction == null) return;
        if (Invincible) return;
        
        var random = new Random();
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {   // Evasion
            // TODO: Evasion Effect
            return;
        }
        
        // 일반적으로 Normal Damage 만 Critical 가능, Magical이나 True Damage Critical 구현 시 데미지를 넣는 Unit으로부터 자체적으로 계산
        var totalDamage = random.Next(100) < attacker.CriticalChance && damageType is Damage.Normal
            ? (int)(damage * attacker.CriticalMultiplier) : damage;
        
        if (ShieldRemain > 0)
        {   
            // Shield
            ShieldRemain -= totalDamage;
            if (ShieldRemain < 0)
            {
                totalDamage = Math.Abs(ShieldRemain);
                ShieldRemain = 0;
            }
        }

        totalDamage = GameManager.Instance.CalcDamage(this, damageType, totalDamage);
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
            
            if (_reflectionFaint && random.Next(100) < ReflectionFaintRate && attacker.Targetable)
            {
                Room.Push(AddBuffAction, BuffId.Fainted,
                    BuffParamType.None, attacker, this, 0, 1000, false);
            }
        }
    }
}
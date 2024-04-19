using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonBomb : SnowBomb
{
    private bool _addicted;
    private bool _recoverMp;
    private bool _attackSpeedBuff;
    private readonly int _recoverMpParam = 15;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.PoisonBombAdjacentAddicted:
                    _addicted = true;
                    break;
                case Skill.PoisonBombAdjacentRecoverMp:
                    _recoverMp = true;
                    break;
                case Skill.PoisonBombAdjacentAttackSpeed:
                    _attackSpeedBuff = true;
                    break;
            }
        }
    }

    public override void SetEffectEffect()
    {
        var enemyList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var allyList = new[] { GameObjectType.Monster };
        var enemyGameObjects = Room.FindTargets(this, enemyList, SkillRange);
        var allyGameObjects = Room.FindTargets(this, allyList, SkillRange);
        
        foreach (var gameObject in enemyGameObjects)
        {
            gameObject.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            BuffManager.Instance.AddBuff(BuffId.Burn, gameObject, this, 0, 5);
            if (_addicted)
            {
                BuffManager.Instance.AddBuff(BuffId.Addicted, gameObject, this, 0, 5);
            }
        }

        foreach (var gameObject in allyGameObjects)
        {
            if (_recoverMp)
            {
                gameObject.Mp += _recoverMpParam;
            }
            if (_attackSpeedBuff)
            {
                BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, gameObject, this, 15, 5);
            }
        }
        OnExplode(Attacker);
    }

    public override void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    {
        var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var gameObjects = Room.FindTargets(target, targetList, Area);
        foreach (var gameObject in gameObjects)
        {
            gameObject.OnDamaged(this, TotalSkillDamage, Damage.Normal);
        }
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
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
                int refParam = (int)(totalDamage * ReflectionRate);
                attacker.OnDamaged(this, refParam, damageType, true);
            }
        }
        else
        {
            totalDamage = damage;
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp > 0) return;
        OnGoingToExplode(attacker);
    }
}
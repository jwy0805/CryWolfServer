using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonBomb : SnowBomb
{
    private bool _addicted;
    private bool _recoverMp;
    private bool _doubleBomb;
    private readonly int _recoverMpParam = 15;
    private GameObject _subTarget;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.PoisonBombPoison:
                    _addicted = true;
                    break;
                case Skill.PoisonBombAdjacentRecoverMp:
                    _recoverMp = true;
                    break;
                case Skill.PoisonBombDoubleBomb:
                    _doubleBomb = true;
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
            BuffManager.Instance.AddBuff(BuffId.Burn, gameObject, this, 0, 5000);
        }

        foreach (var gameObject in allyGameObjects)
        {
            if (_recoverMp == false) continue;
            gameObject.Mp += _recoverMpParam;
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
            if (_addicted) BuffManager.Instance.AddBuff(BuffId.Addicted, gameObject, this, 0, 5000);
        }
    }
    
    public override void SetAdditionalProjectileEffect(GameObject target)
    {
        if (_doubleBomb == false) return;
        _subTarget = Room.FindRandomTarget(this, SkillRange, AttackType);
        var projectile = ObjectManager.Instance.CreateProjectile(ProjectileId.SnowBombSkill);
        projectile.Room = Room;
        projectile.PosInfo = PosInfo;
        // projectile.PosInfo.PosY = attacker.PosInfo.PosY + attacker.Stat.SizeY;
        projectile.Info.PosInfo = projectile.PosInfo;
        projectile.Info.Name = ProjectileId.PoisonBombSkill.ToString();
        projectile.Target = _subTarget;
        projectile.Parent = this;
        projectile.Attack = TotalAttack;
        projectile.Init();
        Room.Push(Room.EnterGame, projectile);
        SetNextState();
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
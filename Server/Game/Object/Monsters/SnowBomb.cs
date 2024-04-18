using Google.Protobuf.Protocol;

namespace Server.Game;

public class SnowBomb : Bomb
{
    private bool _areaAttack = false;
    private bool _burn = false;
    private bool _adjacentDamage = false;
    private readonly float _area = 1.5f;
    private int _readyToExplode = 0;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnowBombFireResist:
                    FireResist += 20;
                    break;
                case Skill.SnowBombAreaAttack:
                    _areaAttack = true;
                    break;
                case Skill.SnowBombBurn:
                    _burn = true;
                    break;
                case Skill.SnowBombAdjacentDamage:
                    _adjacentDamage = true;
                    break;
            }
        }
    }
    
    public bool ExplosionBurn => _burn;

    public override void SetNormalAttackEffect(GameObject target)
    {
        base.SetNormalAttackEffect(target);
        if (!_areaAttack) return;
        var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var gameObjects = Room.FindTargets(target, targetList, _area);
        foreach (var gameObject in gameObjects)
        {
            gameObject.OnDamaged(this, TotalSkillDamage, Damage.Normal);
        }
    }

    public override void SetNextState(State state)
    {
        if (state == State.GoingToExplode && _readyToExplode == 2)
        {
            State = State.Explode;
            BroadcastPos();
        }
        else
        {
            _readyToExplode++;
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
        if (_adjacentDamage) OnGoingToExplode(attacker);
        else OnDead(attacker);
    }

    protected virtual void OnGoingToExplode(GameObject attacker)
    {
        if (Room == null) return;
        Targetable = false;
        if (attacker.Target != null)
        {
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
            {
                if (attacker.Parent != null)
                {
                    attacker.Parent.Target = null;
                    attacker.State = State.Idle;
                    BroadcastPos();
                }
            }
            attacker.Target = null;
            attacker.State = State.Idle;
            BroadcastPos();
        }
        
        var statePacket = new S_State { ObjectId = Id, State = State.GoingToExplode };
        Room.Broadcast(statePacket);
    }
}
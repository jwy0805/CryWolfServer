using Google.Protobuf.Protocol;

namespace Server.Game;

public class SnowBomb : Bomb
{
    private bool _areaAttack = false;
    private bool _burn = false;
    private bool _adjacentDamage = false;
    private int _readyToExplode = 0;

    protected readonly float Area = 1.5f;
    protected GameObject Attacker;
    
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
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 15;
        }

        if (Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastPos();
            UpdateSkill();
            Mp = 0;
        }
        else
        {
            switch (State)
            {
                case State.Die:
                    UpdateDie();
                    break;
                case State.Moving:
                    UpdateMoving();
                    break;
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Skill:
                    UpdateSkill();
                    break;
                case State.KnockBack:
                    UpdateKnockBack();
                    break;
                case State.Faint:
                    break;
                case State.Standby:
                    break;
            }   
        }
    }
    
    public override void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    {
        if (pId == ProjectileId.BombProjectile)
        {
            target.OnDamaged(this, TotalAttack, Damage.Normal);
        }
        else
        {
            if (_areaAttack)
            {
                var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
                var gameObjects = Room.FindTargets(target, targetList, Area);
                foreach (var gameObject in gameObjects)
                {
                    gameObject.OnDamaged(this, TotalSkillDamage, Damage.Normal);
                }
            }
            else
            {
                target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            }
        }
    }

    public override void SetEffectEffect()
    {
        var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var gameObjects = Room.FindTargets(this, targetList, SkillRange);
        foreach (var gameObject in gameObjects)
        {
            gameObject.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            if (!_burn) continue;
            BuffManager.Instance.AddBuff(BuffId.Burn, gameObject, this, 0, 5000);
            OnExplode(Attacker);
        }
    }

    public override void SetNextState(State state)
    {
        base.SetNextState(state);
        
        if (state == State.GoingToExplode && _readyToExplode > 2)
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
        Attacker = attacker;
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

        State = State.GoingToExplode;
        BroadcastPos();
    }

    public virtual void OnExplode(GameObject attacker)
    {
        S_Die diePacket = new() { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
    }
}
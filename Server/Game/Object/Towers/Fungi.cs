using Google.Protobuf.Protocol;

namespace Server.Game;

public class Fungi : Mushroom
{
    private bool _poison;
    private bool _closestHeal;
    private readonly float _closestHealRange = 3;
    private readonly float _closestHealParam = 120;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.FungiPoison:
                    _poison = true;
                    break;
                case Skill.FungiPoisonResist:
                    PoisonResist += 20;
                    break;
                case Skill.FungiClosestHeal:
                    _closestHeal = true;
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
        Player.SkillSubject.SkillUpgraded(Skill.FungiPoison);
        Player.SkillSubject.SkillUpgraded(Skill.FungiClosestHeal);
    }
    
    public override void Update()
    {
        base.Update();
        FindClosestMushroom();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(
                _poison ? ProjectileId.FungiProjectile : ProjectileId.BasicProjectile4, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        if (pid == ProjectileId.FungiProjectile)
        {
            Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
            if (_poison)
            {
                Room.Push(AddBuffAction, BuffId.Addicted,
                    BuffParamType.None, target, this, 0.03f, 5000, false);
            }
        }
        else
        {
            base.ApplyProjectileEffect(target, pid);
        }
    }
    
    protected override void OnDead(GameObject? attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
        if (Room == null || AddBuffAction == null) return;
        
        Targetable = false;
        if (attacker != null)
        {
            attacker.KillLog = Id;
            if (attacker.Target != null)
            {
                if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
                {
                    if (attacker.Parent != null) attacker.Parent.Target = null;
                }
                attacker.Target = null;
            }
        }

        if (_closestHeal)
        {
            var fungus = Room.FindTargetsBySpecies(this, GameObjectType.Tower,
                new List<UnitId> { UnitId.Mushroom, UnitId.Fungi, UnitId.Toadstool }, _closestHealRange);
            if (fungus.Count == 0) return;
            foreach (var fungi in fungus)
            {
                Room.Push(AddBuffAction, BuffId.HealBuff,
                    BuffParamType.Constant, fungi, this, _closestHealParam, 0, false);
            }
        }
        
        if (AlreadyRevived == false && WillRevive)
        {
            S_Die dieAndRevivePacket = new() { ObjectId = Id, Revive = true };
            Room.Broadcast(dieAndRevivePacket);
            return;
        }

        Room.Broadcast(new S_Die { ObjectId = Id });
        Room.DieTower(Id);
    }
}
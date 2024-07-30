using Google.Protobuf.Protocol;

namespace Server.Game;

public class Snake : Snakelet
{
    private bool _fire = false;
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeFire:
                    _fire = true;
                    break;
                case Skill.SnakeAccuracy:
                    Accuracy += 25;
                    break;
                case Skill.SnakeFireResist:
                    FireResist += 20;
                    break;
                case Skill.SnakeSpeed:
                    MoveSpeed += 1;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(_fire ? ProjectileId.SnakeFire : ProjectileId.BasicProjectile2
                , this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        if (_fire)
        {
            Room.Push(AddBuffAction, BuffId.Burn, BuffParamType.None, target, this, 0, 5000, false);
        }
    }
}
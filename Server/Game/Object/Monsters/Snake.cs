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
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(_fire ? ProjectileId.SnakeFire : ProjectileId.BasicProjectile2
                , this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        if (_fire) BuffManager.Instance.AddBuff(BuffId.Burn, target, this, 0, 5000);
    }
}
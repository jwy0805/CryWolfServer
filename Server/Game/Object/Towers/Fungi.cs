using Google.Protobuf.Protocol;

namespace Server.Game;

public class Fungi : Mushroom
{
    private bool _poison;
    
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
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(
                _poison ? ProjectileId.FungiProjectile : ProjectileId.BasicProjectile4, this, 5f);
        });
    }
}
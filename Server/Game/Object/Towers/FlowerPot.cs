using Google.Protobuf.Protocol;

namespace Server.Game;

public class FlowerPot : Sprout
{
    private bool _3Hit;
    private bool _recoverBurn;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.FlowerPot3Hit:
                    _3Hit = true;
                    break;
                case Skill.FlowerPotRecoverBurn:
                    _recoverBurn = true;
                    break;
            }
        }
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.SproutFire, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        BuffManager.Instance.AddBuff(BuffId.Burn, BuffParamType.None, target, this, 0, 5000);
        var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        Hp += (int)(damage * DrainParam);
        BroadcastHp();
    }
}
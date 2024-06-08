using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoPester : MosquitoBug
{
    private bool _poison = false;
    private bool _woolDown = false;
    
    protected int WoolDownRate = 20;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoPesterPoison:
                    _poison = true;
                    CurrentProjectile = ProjectileId.MosquitoPesterProjectile;
                    break;
                case Skill.MosquitoPesterWoolRate:
                    _woolDown = true;
                    break;
                case Skill.MosquitoPesterPoisonResist:
                    PoisonResist += 20;
                    break;
                case Skill.MosquitoPesterEvasion:
                    Evasion += 15;
                    break;
                case Skill.MosquitoPesterHealth:
                    MaxHp += 30;
                    Hp += 30;
                    BroadcastHealth();
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        CurrentProjectile = ProjectileId.BasicProjectile2;
    }

    protected override async void AttackImpactEvents(long impactTime)
    {
        if (Target == null || Room == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            Room.SpawnProjectile(CurrentProjectile, this, 5f);
        });
    }

    public override void ApplyProjectileEffect(GameObject? target)
    {
        target?.OnDamaged(this, TotalAttack, Damage.Normal);
        
        if (CurrentProjectile == ProjectileId.MosquitoPesterProjectile)
        {
            if (target is Creature _)
            {
                BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1000);
                if (_poison)
                {
                    BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0, 5000);
                }
            }
        }
        
        if (target is not Sheep sheep) return;
        if (_woolDown) sheep.YieldDecrement = sheep.Resource * WoolDownRate / 100;
        BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1000);
    }
}
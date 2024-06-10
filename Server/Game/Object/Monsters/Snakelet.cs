using Google.Protobuf.Protocol;

namespace Server.Game;

public class Snakelet : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeletAttackSpeed:
                    AttackSpeed += AttackSpeed * 0.2f;
                    break;
                case Skill.SnakeletAttack:
                    Attack += 6;
                    break;
                case Skill.SnakeletEvasion:
                    Evasion += 15;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        AttackImpactMoment = 0.25f;
        CurrentProjectile = ProjectileId.BasicProjectile;
    }

    protected override async void AttackImpactEvents(long impactTime)
    {
        if (Target == null || Room == null || Hp <= 0) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(CurrentProjectile, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || Hp <= 0) return;
        target?.OnDamaged(this, TotalAttack, Damage.Normal);
    }
}
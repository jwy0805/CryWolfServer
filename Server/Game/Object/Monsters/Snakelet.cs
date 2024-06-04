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
        AttackImpactTime = 0.25f;
        CurrentProjectile = ProjectileId.BasicProjectile;
    }

    protected override async void AttackImpactEvents(long impactTime)
    {
        if (Target == null) return;
        await Scheduler.ScheduleEvent(impactTime, () => Room.SpawnProjectile(CurrentProjectile, this));
    }
    
    public override void ApplyProjectileEffect(GameObject? target)
    {
        target?.OnDamaged(this, TotalAttack, Damage.Normal);
    }
}
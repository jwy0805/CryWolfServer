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
                    BroadcastHp();
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
        Player.SkillSubject.SkillUpgraded(Skill.MosquitoPesterPoison);
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(_poison ? ProjectileId.MosquitoPesterProjectile : ProjectileId.BasicProjectile2, 
                this, 5f);
        });
    }

    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        
        if (new Random().Next(100) < FaintParam)
        {
            Room.Push(AddBuffAction, BuffId.Fainted,
                BuffParamType.None, target, this, 0, 5000, false);
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        
        if (pid == ProjectileId.MosquitoPesterProjectile)
        {
            if (target is Creature && _poison)
            {
                // Poison
                Room.Push(AddBuffAction, BuffId.Addicted, 
                    BuffParamType.Percentage, target, this, 0.05f, 5000, false);
            }
        }
        
        // WoolDown
        if (target is not Sheep sheep) return;
        if (_woolDown) sheep.YieldDecrement = sheep.Resource * WoolDownRate / 100;
    }
}
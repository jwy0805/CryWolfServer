using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class MothCelestial : MothMoon
{
    private bool _poison;
    private bool _breedSheep;
    private bool _debuffRemove;
    private readonly int _breedProb = 10;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothCelestialSheepHealParamUp:
                    HealParam = 300;
                    break;
                case Skill.MothCelestialPoison:
                    _poison = true;
                    break;
                case Skill.MothCelestialAccuracy:
                    Accuracy += 30;
                    break;
                case Skill.MothCelestialBreed:
                    _breedSheep = true;
                    break;
                case Skill.MothCelestialSheepDebuffRemove:
                    _debuffRemove = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Supporter;
        
        Player.SkillSubject.SkillUpgraded(Skill.MothCelestialSheepHealParamUp);
        Player.SkillSubject.SkillUpgraded(Skill.MothCelestialPoison);
        Player.SkillSubject.SkillUpgraded(Skill.MothCelestialAccuracy); 
        Player.SkillSubject.SkillUpgraded(Skill.MothCelestialBreed);
        Player.SkillSubject.SkillUpgraded(Skill.MothCelestialSheepDebuffRemove);
    }
    
    protected override void UpdateIdle()
    {   // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 flatTargetPos = Target.CellPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);

        if (distance > TotalAttackRange) return;
        State = Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(_poison ? ProjectileId.MothCelestialPoison : ProjectileId.MothMoonProjectile,
                this, 5f);
        });
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            
            var types = new[] { GameObjectType.Sheep };
            var sheeps = Room.FindTargets(this, types, TotalSkillRange);

            if (sheeps.Any() == false) return;
            
            // Heal Sheep
            var sheepHeal = sheeps.MinBy(sheep => sheep.Hp);
            if (sheepHeal != null) sheepHeal.Hp += HealParam;
            
            // Shield Sheep
            var sheepShield = sheeps.MinBy(_ => Guid.NewGuid());
            if (sheepShield != null) sheepShield.ShieldAdd += ShieldParam;

            // Debuff Remove
            if (_debuffRemove)
            {
                var sheepDebuff = BuffManager.Instance.Buffs.Where(buff => buff.Master is Sheep)
                    .Select(buff => buff.Master as Sheep)
                    .Distinct()
                    .Where(s => s != null && Vector3.Distance(s.CellPos with { Y = 0 }, CellPos with { Y = 0 }) <= TotalSkillRange)
                    .ToList();
                
                foreach (var sheep in sheepDebuff)
                {
                    if (sheep is { Room: not null }) BuffManager.Instance.RemoveAllDebuff(sheep);
                }
            }
            else
            {
                var sheep = BuffManager.Instance.Buffs.Where(buff => buff.Master is Sheep)
                    .Select(buff => buff.Master as Sheep)
                    .Distinct()
                    .Where(s => s != null && Vector3.Distance(s.CellPos with { Y = 0 }, CellPos with { Y = 0 }) <= TotalSkillRange)
                    .MinBy(_ => Guid.NewGuid());
                
                if (sheep is { Room: not null }) BuffManager.Instance.RemoveAllDebuff(sheep);
            }
            
            // Breed Sheep
            if (_breedSheep && new Random().Next(99) < _breedProb)
            {
                Room.EnterSheepByServer(Player);
            }

            Mp = 0;
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        if (_poison) BuffManager.Instance.AddBuff(BuffId.Addicted, BuffParamType.Percentage,
            target, this, 0.05f, 5000);
    }
    
    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            AttackEnded = true;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);  

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
            AttackEnded = true;
        }
        else
        {
            State = Mp >= MaxMp ? State.Skill : State.Attack;
            SyncPosAndDir();
        }
    }
}
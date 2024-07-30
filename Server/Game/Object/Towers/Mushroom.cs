using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class Mushroom : Tower
{
    private bool _closestAttack;
    private int _currentMushroomId;
    
    protected readonly int ClosestAttackParam = 4;
    protected HashSet<int> CurrentSet = new();

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MushroomAttack:
                    Attack += 3;
                    break;
                case Skill.MushroomRange:
                    AttackRange += 1;
                    break;
                case Skill.MushroomClosestAttack:
                    _closestAttack = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
    }

    public override void Update()
    {
        base.Update();
        if (_closestAttack) FindClosestMushroom();
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.BasicProjectile4, this, 5f);
        });
    }
    
    protected void FindClosestMushroom()
    {
        if (Room == null) return;
        var unitIds = new List<UnitId> { UnitId.Mushroom, UnitId.Fungi, UnitId.Toadstool };
        var otherMushrooms = Room
            .FindTargetsBySpecies(this, GameObjectType.Tower, unitIds, TotalAttackRange);
        if (otherMushrooms.Count == 0) return;

        var mushroom = otherMushrooms
            .Where(mushroom => mushroom.Id != Id)
            .MinBy(mushroom => Vector3.Distance(CellPos, mushroom.CellPos));
        if (mushroom == null) return;
        
        if (_currentMushroomId == mushroom.Id) return;
        // 새 유닛에 버프 적용
        mushroom.AttackParam += ClosestAttackParam;
        // 기존 유닛 버프 제거
        var previousMushroom = Room.FindGameObjectById(_currentMushroomId);
        if (previousMushroom != null) previousMushroom.AttackParam -= ClosestAttackParam;
        // 업데이트
        _currentMushroomId = mushroom.Id;
    }
}
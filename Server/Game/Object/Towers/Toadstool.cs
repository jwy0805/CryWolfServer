using Google.Protobuf.Protocol;

namespace Server.Game;

public class Toadstool : Fungi
{
    private bool _closestAttackAll;
    private bool _poisonCloud;
    private readonly int _closestAttackAllParam = 5;
    
    public bool NestedPoison { get; set; }
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.ToadstoolClosestAttackAll:
                    _closestAttackAll = true;
                    break;
                case Skill.ToadstoolPoisonResist:
                    PoisonResist += 30;
                    break;
                case Skill.ToadstoolNestedPoison:
                    NestedPoison = true;
                    break;
                case Skill.ToadstoolPoisonCloud:
                    _poisonCloud = true;
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
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (_closestAttackAll) FindMushrooms();
        else FindClosestMushroom();
        
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
        }

        // Console.WriteLine(State);
        switch (State)
        {
            case State.Die:
                UpdateDie();
                break;
            case State.Idle:
                UpdateIdle();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Skill:
                UpdateSkill();
                break;
            case State.KnockBack:
                UpdateKnockBack();
                break;
            case State.Faint:
            case State.Standby:
                break;
        }
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.ToadstoolProjectile, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0 || AddBuffAction == null) return;
        
        if (Mp >= MaxMp && _poisonCloud)
        {
            var effectPos = new PositionInfo
            {
                PosX = target.CellPos.X, PosY = target.CellPos.Y, PosZ = target.CellPos.Z
            };
            Room.SpawnEffect(EffectId.PoisonCloud, this, this, effectPos, false, 3000);
            Mp = 0;
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        Room.Push(AddBuffAction, BuffId.Addicted,
            BuffParamType.Percentage, target, this, 0.05f, 5000, NestedPoison);
    }

    private void FindMushrooms()
    {   
        // TotalSkillRange 내 모든 버섯 찾기
        if (Room == null) return;
        var unitIds = new List<UnitId> { UnitId.Mushroom, UnitId.Fungi, UnitId.Toadstool };
        var mushrooms = Room
            .FindTargetsBySpecies(this, GameObjectType.Tower, unitIds, TotalSkillRange);
        if (mushrooms.Count == 0) return;
        
        var newSet = new HashSet<int>(mushrooms
            .Where(mushroom => mushroom.Id != Id)
            .Select(mushroom => mushroom.Id));
        
        // 추가된 버섯에 버프 적용
        foreach (var mushroomId in newSet.Where(mushroomId => CurrentSet.Contains(mushroomId) == false))
        {
            if (Room.FindGameObjectById(mushroomId) is Creature creature) 
                creature.AttackParam += _closestAttackAllParam;
        }
        
        // 제거된 버섯에서 버프 제거
        foreach (var mushroomId in CurrentSet.Where(mushroomId => newSet.Contains(mushroomId) == false))
        {
            if (Room.FindGameObjectById(mushroomId) is Creature creature) 
                creature.AttackParam -= _closestAttackAllParam;
        }
        
        // 현재 상태를 새로운 상태로 업데이트
        CurrentSet = newSet;
    }
}
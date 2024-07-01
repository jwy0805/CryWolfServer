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
        Player.SkillSubject.SkillUpgraded(Skill.ToadstoolPoisonCloud);
        Player.SkillSubject.SkillUpgraded(Skill.ToadstoolNestedPoison);
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
        }
        
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
        
        if (_closestAttackAll) FindMushrooms();
        else FindClosestMushroom();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.ToadstoolProjectile, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0) return;
        
        if (Mp >= MaxMp && _poisonCloud)
        {
            var effectPos = new PositionInfo
            {
                PosX = target.CellPos.X, PosY = target.CellPos.Y, PosZ = target.CellPos.Z
            };
            Room.SpawnEffect(EffectId.PoisonCloud, this, effectPos, false, 3000);
            Mp = 0;
        }
        
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        BuffManager.Instance.AddBuff(BuffId.Addicted, BuffParamType.None, 
            target, this, 0.03f, 5000, NestedPoison);
    }

    private void FindMushrooms()
    {   // TotalSkillRange 내 모든 버섯 찾기
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
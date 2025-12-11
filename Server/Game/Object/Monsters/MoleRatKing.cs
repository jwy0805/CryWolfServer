using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class MoleRatKing : MoleRat
{
    private readonly List<GameObjectType> _typeList = new() { GameObjectType.Sheep, GameObjectType.Tower };
    private bool _burrow = false;
    private bool _stealWool = false;
    
    private readonly float _stealWoolParam = DataManager.SkillDict[(int)Skill.MoleRatKingStealWool].Value;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MoleRatKingBurrow:
                    _burrow = true;
                    BurrowEvasionParam = (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.MoleRatKingStealWool:
                    _stealWool = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Warrior;
        IdleToRushAnimTime = StdAnimTime * 2 / 3;
        RushToIdleAnimTime = StdAnimTime * 5 / 6;
    }
    
    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestPriorityTarget(this, _typeList);
        if (Target == null) return;
        State = _burrow ? State.IdleToUnderground : State.IdleToRush;
    }

    protected override void UpdateRush()
    {
        if (_burrow) State = State.IdleToUnderground;
        base.UpdateRush();
    }

    protected override void UpdateAttack2()
    {
        UpdateAttack();
    }

    protected override void UpdateUnderground()
    {
        if (Room == null) return;
        
        // Targeting 우선순위 - Sheep
        var targetTypeList = new List<GameObjectType> { GameObjectType.Sheep };
        Target = Room.FindClosestPriorityTarget(this, targetTypeList, Stat.AttackType, false);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   
            // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(this, Target);
        float distance = Vector3.Distance(DestPos, CellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        if (distance <= AttackRange)
        {
            State = State.UndergroundToIdle;
            return;
        }
        
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this, false);
        BroadcastPath();
    }
    
    protected override void UpdateIdleToUnderground()
    {
        MotionChangeEvents(IdleToRushAnimTime);
    }
    
    protected override void UpdateUndergroundToIdle()
    {
        MotionChangeEvents(RushToIdleAnimTime);
    }

    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0 || Target.Room == null)
        {
            State = State.Idle;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);  

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
        }
        else
        {
            State = GetRandomState(State.Attack, State.Attack2);
            SyncPosAndDir();
        }
    }
    
    public override void SetNextState(State state)
    {
        if (state == State.Die && WillRevive)
        {
            State = State.Idle;
            Hp = (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHp();
            // 부활 Effect 추가
        }
        
        if (state == State.IdleToRush)
        {
            State = State.Rush;
            if (StateChanged)
            {
                MoveSpeedParam += BurrowSpeedParam;
                EvasionParam += BurrowEvasionParam; 
            }
        }
        
        if (state == State.IdleToUnderground)
        {
            State = State.Underground;
            if (StateChanged)
            {
                MoveSpeedParam += BurrowSpeedParam;
                EvasionParam += BurrowEvasionParam;
            }
        }

        if (state == State.RushToIdle)
        {
            State = GetRandomState(State.Attack, State.Attack2);
            SyncPosAndDir();
            MoveSpeedParam -= BurrowSpeedParam;
            EvasionParam -= BurrowEvasionParam;
        }

        if (state == State.UndergroundToIdle)
        {
            State = GetRandomState(State.Attack, State.Attack2);
            SyncPosAndDir();
            MoveSpeedParam -= BurrowSpeedParam;
            EvasionParam -= BurrowEvasionParam;
        }
    }
    
    public override void ApplyAttackEffect(GameObject target)
    {
        if (Room == null) return;
        
        var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
        Hp += (int)(damage * DrainParam);
        BroadcastHp();
        
        // Steal Wool 처리
        if (_stealWool)
        {
            if (target is Sheep sheep)
            {
                var stealWool = (int)(Room.GameInfo.TotalSheepYield * _stealWoolParam);
                sheep.YieldDecrement += stealWool;
                Room.GameInfo.WolfResource += stealWool;
                Room.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.MoleRatKingSteal, SoundType = SoundType.D3 });
            }
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        Room.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.MonsterAttack, SoundType = SoundType.D3 });

        // Steal Attack 처리
        if (target.Room == null || target.Targetable == false || target.Hp <= 0) return;
        if (StolenObjectId == 0)
        {
            StolenDamage = (int)(target.TotalAttack * StealAttackParam);
            AttackParam += StolenDamage;
            target.AttackParam -= AttackParam;
            StolenObjectId = target.Id;
            return;
        }
        
        if (StolenObjectId == target.Id) return;
        var stolenTarget = Room.FindGameObjectById(StolenObjectId);
        if (stolenTarget == null) return;
        
        stolenTarget.AttackParam += StolenDamage;
        StolenDamage = (int)(target.TotalAttack * StealAttackParam);
        target.AttackParam -= StolenDamage;
        StolenObjectId = target.Id;
    }
}
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class MoleRatKing : MoleRat
{
    private List<GameObjectType> _typeList = new() { GameObjectType.Sheep };
    private bool _burrow = false;
    private bool _stealWool = false;
    private readonly float _stealWoolParam = 0.1f;
    
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
        IdleToRushAnimTime = StdAnimTime * 2 / 3;
        RushToIdleAnimTime = StdAnimTime * 5 / 6;
    }
    
    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this, _typeList);
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
    {   // Targeting 우선순위 - Sheep
        var targetTypeList = new List<GameObjectType> { GameObjectType.Sheep };
        Target = Room.FindClosestTarget(this, targetTypeList);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
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
    
    public override void SetNextState()
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
            State = GetRandomState(State.Attack, State.Attack2);
            SetDirection();
        }
    }
    
    public override void SetNextState(State state)
    {
        if (state == State.Die && WillRevive)
        {
            State = State.Idle;
            Hp = (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHealth();
            // 부활 Effect 추가
        }
        
        if (state == State.IdleToRush)
        {
            State = State.Rush;
            if (StateChanged)
            {
                MoveSpeedParam += 2;
                EvasionParam += 30; 
            }
        }
        
        if (state == State.IdleToUnderground)
        {
            State = State.Underground;
            if (StateChanged)
            {
                MoveSpeedParam += 2;
                EvasionParam += 40;
            }
        }

        if (state == State.RushToIdle)
        {
            State = GetRandomState(State.Attack, State.Attack2);
            SetDirection();
            MoveSpeedParam -= 2;
            EvasionParam -= 30;
        }

        if (state == State.UndergroundToIdle)
        {
            State = GetRandomState(State.Attack, State.Attack2);
            SetDirection();
            MoveSpeedParam -= 2;
            EvasionParam -= 40;
        }
    }
    
    public override void ApplyAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        Hp += (int)(Math.Max(TotalAttack - target.TotalDefence, 0) * DrainParam);
        // Steal Attack 처리
        if (StolenObjectId == 0)
        {
            StolenDamage = (int)(target.TotalAttack * StealAttackParam);
            AttackParam += StolenDamage;
            target.AttackParam -= AttackParam;
            StolenObjectId = target.Id;
            return;
        }
        
        if (StolenObjectId == target.Id) return;
        
        var stolenTarget = Room?.FindGameObjectById(StolenObjectId);
        if (stolenTarget == null) return;
        stolenTarget.AttackParam += StolenDamage;
        
        StolenDamage = (int)(target.TotalAttack * StealAttackParam);
        target.AttackParam -= StolenDamage;
        
        StolenObjectId = target.Id;
        // Steal Wool 처리
        if (_stealWool == false) return;
        if (target is not Sheep sheep) return;
        int stealWool = (int)(Room.GameInfo.SheepYield * _stealWoolParam);
        sheep.YieldDecrement += stealWool;
        // TODO : 훔친만큼 DNA 증가
    }
}
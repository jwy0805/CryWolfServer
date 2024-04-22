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
    private float _stealWoolParam = 0.1f;
    
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
        AttackSpeedReciprocal = 5 / 6f;
        AttackSpeed *= AttackSpeedReciprocal;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

        if (MaxMp != 1 && Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastPos();
            UpdateSkill();
            Mp = 0;
        }
        else
        {
            switch (State)
            {
                case State.Die:
                    UpdateDie();
                    break;
                case State.Moving:
                    UpdateMoving();
                    break;
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Rush:
                    UpdateRush();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Skill:
                    UpdateSkill();
                    break;
                case State.Skill2:
                    UpdateSkill2();
                    break;
                case State.KnockBack:
                    UpdateKnockBack();
                    break;
                case State.Faint:
                    break;
                case State.Standby:
                    break;
                case State.Underground:
                    UpdateUnderground();
                    break;
            }   
        }
    }
    
    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this, _typeList);
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        if (Target == null) return;
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        
        (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
        BroadcastDest();
        
        State = _burrow ? State.IdleToUnderground : State.IdleToRush;
        BroadcastPos();
    }

    protected override void UpdateRush()
    {
        if (_burrow) State = State.IdleToUnderground;
        base.UpdateRush();
    }

    private void UpdateUnderground()
    {
        // Targeting
        Target = Room.FindClosestTarget(this, _typeList);
        if (Target != null)
        {
            // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
            Vector3 position = CellPos;
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
            double deltaX = DestPos.X - CellPos.X;
            double deltaZ = DestPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= AttackRange)
            {
                CellPos = position;
                State = State.UndergroundToIdle;
                BroadcastPos();
                return;
            }

            // Target이 있으면 이동
            DestPos = Room.Map.GetClosestPoint(CellPos, Target);
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos, false);
            BroadcastDest();
        }
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            BroadcastPos();
        }
    }
    
    public override void SetNextState(State state)
    {
        if (state == State.IdleToRush)
        {
            State = State.Rush;
            BroadcastPos();
            MoveSpeedParam += 2;
            EvasionParam += 30;
        }
        
        if (state == State.IdleToUnderground)
        {
            State = State.Underground;
            BroadcastPos();
        }

        if (state is State.RushToIdle or State.UndergroundToIdle)
        {
            State = State.Attack;
            BroadcastPos();
            MoveSpeedParam -= 2;
            EvasionParam -= 30;
        }
    }
    
    public override void SetNormalAttackEffect(GameObject target)
    {
        base.SetNormalAttackEffect(target);
        if (_stealWool == false) return;
        if (target is not Sheep sheep) return;
        int stealWool = (int)(Room.GameInfo.SheepYield * _stealWoolParam);
        sheep.YieldDecrement += stealWool;
        // TODO : 훔친만큼 DNA 증가
    }
}
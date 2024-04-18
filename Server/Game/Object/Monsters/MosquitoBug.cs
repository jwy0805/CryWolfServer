using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MosquitoBug : Monster
{
    private bool _faint = false;
    
    protected List<GameObjectType> _typeList = new() { GameObjectType.Sheep };
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoBugEvasion:
                    Evasion += 10;
                    break;
                case Skill.MosquitoBugRange:
                    AttackRange += 1;
                    break;
                case Skill.MosquitoBugSpeed:
                    MoveSpeed += 1;
                    break;
                case Skill.MosquitoBugSheepFaint:
                    _faint = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        AttackSpeedReciprocal = 4 / 5f;
        AttackSpeed *= AttackSpeedReciprocal;
    }

    protected override void UpdateIdle()
    {
        Target = Room.FindClosestTarget(this, _typeList, 2) 
                 ?? Room.FindClosestTarget(this, 2);
        LastSearch = Room.Stopwatch.Elapsed.Milliseconds;
        if (Target == null) return;
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);

        (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
        BroadcastDest();
        
        State = State.Moving;
        BroadcastPos();
    }

    protected override void UpdateMoving()
    {
        // Targeting
        Target = Room.FindClosestTarget(this, _typeList, 2) 
                 ?? Room.FindClosestTarget(this, 2);
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
                State = State.Attack;
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

    public override void SetNormalAttackEffect(GameObject target)
    {
        base.SetNormalAttackEffect(target);
        if (target is Sheep _ && _faint)
        {
            BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1);
        }
    }
}
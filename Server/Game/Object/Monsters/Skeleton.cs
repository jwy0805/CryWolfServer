using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Skeleton : Monster
{
    private bool _defenceDown = false;
    private bool _nestedDebuff = false;

    protected readonly float DefenceDownParam = 3;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SkeletonDefenceDown:
                    _defenceDown = true;
                    break;
                case Skill.SkeletonNestedDebuff:
                    _nestedDebuff = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
    }
    
    protected override void UpdateMoving()
    {   // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            BroadcastPos();
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 position = CellPos;
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        if (distance <= AttackRange)
        {
            CellPos = position;
            State = new Random().Next(2) == 0 ? State.Attack : State.Attack2;
            BroadcastPos();
            return;
        }
        // Target이 있으면 이동
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    public override void ApplyNormalAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        
        if (_defenceDown)
        {
            BuffManager.Instance.AddBuff(BuffId.DefenceDecrease, target, this, DefenceDownParam, 5000);
        }
        if (_nestedDebuff)
        {
            target.DefenceParam -= 3;
        }
    }

    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }

        if (Target.Hp <= 0)
        {
            Target = null;
            State = State.Idle;
            BroadcastPos();
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));

        if (distance <= TotalAttackRange)
        {
            SetDirection();
            State = new Random().Next(2) == 0 ? State.Attack : State.Attack2;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
        else
        {
            DestPos = targetPos;
            State = State.Moving;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
    }
}
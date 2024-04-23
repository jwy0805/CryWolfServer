using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SkeletonGiant : Skeleton
{
    private bool _defenceDebuff = false;
    private bool _attackSteal = false;
    private bool _reviveSelf = false;
    protected readonly int DefenceDebuffParam = 3;
    protected readonly float DebuffRange = 2.5f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SkeletonGiantDefenceDebuff:
                    _defenceDebuff = true;
                    break;
                case Skill.SkeletonGiantAttackSteal:
                    _attackSteal = true;
                    break;
                case Skill.SkeletonGiantMpDown:
                    MaxMp -= 25;
                    break;
                case Skill.SkeletonGiantRevive:
                    _reviveSelf = true;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (_defenceDebuff && Mp >= MaxMp)
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
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Skill:
                    UpdateSkill();
                    break;
                case State.KnockBack:
                    UpdateKnockBack();
                    break;
                case State.Revive:
                    UpdateRevive();
                    break;
                case State.Faint:
                    break;
                case State.Standby:
                    break;
            }   
        }
    }
    
    protected override void UpdateMoving()
    {
        // Targeting
        Target = Room.FindClosestTarget(this);
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

    protected virtual void UpdateRevive()
    {
        
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        target.DefenceParam -= DefenceDebuffParam;
        var effect = Room.EnterEffect(EffectId.SkeletonGiantEffect, this);
        Room.EnterGame(effect);
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
            State = State.Attack;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
        else
        {
            DestPos = targetPos;
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            State = State.Moving;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
    }
    
    public override void SetNextState(State state)
    {
        if (state == State.Die)
        {
            if (AlreadyRevived == false && _reviveSelf)
            {
                AlreadyRevived = true;
                State = State.Revive;
                BroadcastPos();
                return;
            }
        }
        
        if (state == State.Revive)
        {
            State = State.Idle;
            Hp += (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHealth();
            BroadcastPos();
        }
    }

    public override void OnDead(GameObject attacker)
    {
        if (Room == null) return;
        attacker.KillLog = Id;
        Targetable = false;
        
        if (attacker.Target != null)
        {
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
            {
                if (attacker.Parent != null)
                {
                    attacker.Parent.Target = null;
                    attacker.State = State.Idle;
                    // BroadcastPos();
                }
            }
            attacker.Target = null;
            attacker.State = State.Idle;
            // BroadcastPos();
        }
        
        if (AlreadyRevived == false && _reviveSelf)
        {
            S_Die dieAndRevivePacket = new() { ObjectId = Id, AttackerId = attacker.Id, Revive = true};
            Room.Broadcast(dieAndRevivePacket);
            return;
        }

        S_Die diePacket = new() { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
    }

    public override void RunSkill()
    {
        var effect = Room.EnterEffect(EffectId.SkeletonGiantSkill, this, Target?.PosInfo);
        Room.EnterGameParent(effect, Target ?? this);
        var targetTypeList = new HashSet<GameObjectType>
            { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
        var targets = Room.FindTargets(this, targetTypeList, DebuffRange);
        foreach (var target in targets) target.DefenceParam -= DefenceDebuffParam;
        
        if (_attackSteal == false) return;
        foreach (var target in targets)
        {
            BuffManager.Instance.AddBuff(BuffId.AttackDecrease, target, this, 2, 5, true);
            BuffManager.Instance.AddBuff(BuffId.AttackIncrease, this, this, 2, 5, true);
        }
    }
}
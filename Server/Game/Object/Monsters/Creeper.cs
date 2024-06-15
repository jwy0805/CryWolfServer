using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Creeper : Lurker
{
    private bool _rush = false;
    private bool _poison = false;
    private bool _nestedPoison = false;
    private bool _rollDamageUp = false;
    private readonly int _rushSpeed = 4;
    
    protected bool Start = false;
    protected bool SpeedRestore = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CreeperPoison:
                    _poison = true;
                    break;
                case Skill.CreeperRoll:
                    _rush = true;
                    break;
                case Skill.CreeperNestedPoison:
                    _nestedPoison = true;
                    break;
                case Skill.CreeperRollDamageUp:
                    _rollDamageUp = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        // Player.SkillUpgradedList.Add(Skill.CreeperPoison);
    }
    
    public override void Update()
    {
        base.Update();
    }

    protected override void UpdateIdle()
    {
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        
        if (_rush && Start == false)
        {
            Start = true;
            State = State.Rush; 
        }
        else
        {
            State = State.Moving;
        }
    }
    
    protected override void UpdateMoving()
    {
        if (_rush && Start == false)
        {
            Start = true;
            MoveSpeedParam += _rushSpeed;
            State = State.Rush;
            return;
        }
        
        if (_rush && Start && SpeedRestore == false)
        {
            MoveSpeedParam -= _rushSpeed;
            SpeedRestore = true;
        }
        
        // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        // Target이 사정거리 안에 있다가 밖으로 나간 경우 애니메이션 시간 고려하여 Attack 상태로 변경되도록 조정
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        if (distance <= TotalAttackRange)
        {
            if (LastAnimEndTime != 0 && timeNow <= LastAnimEndTime + animPlayTime) return;
            State = State.Attack;
            SetDirection();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    protected override void UpdateRush()
    {
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        // Roll 충돌 처리
        if (distance <= Stat.SizeX * 0.25 + 0.5f)
        {
            SetRollEffect(Target);
            Mp += MpRecovery;
            State = State.KnockBack;
            DestPos = CellPos + (-Vector3.Normalize(Target.CellPos - CellPos) * 3);
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected override void UpdateKnockBack()
    {
        (Path, Atan) = Room.Map.KnockBack(this, Dir);
        BroadcastPath();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) { return; }
            Room.SpawnProjectile(_nestedPoison ? ProjectileId.BigPoison : 
                _poison ? ProjectileId.SmallPoison : ProjectileId.BasicProjectile, this, 5f);            
        });
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0) return;
        
        if (_poison)
        {
            BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0, 5000);
        }
        else if (_nestedPoison)
        {
            BuffManager.Instance.AddBuff(BuffId.DeadlyAddicted, target, this, 0, 5000);
        }
        
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }

    protected virtual void SetRollEffect(GameObject target)
    {
        if (_rollDamageUp)
        {
            target.OnDamaged(this, TotalSkillDamage * 2, Damage.Normal);
        }
        else
        {
            target.OnDamaged(this, TotalSkillDamage, Damage.Normal);
        }
    }

    // public override void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    // {
    //     target.OnDamaged(this, TotalAttack, Damage.Normal);
    //     if (_poison)
    //     {
    //         BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0, 5000);
    //     }
    //     else if (_nestedPoison)
    //     {
    //         BuffManager.Instance.AddBuff(BuffId.DeadlyAddicted, target, this, 0, 5000);
    //
    //     }
    // }
}
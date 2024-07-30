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
    private readonly int _divideDuration = 800;
    
    protected bool Rushed = false;
    protected readonly int RushSpeed = 4;
    protected readonly float BounceParam = 1f;
    
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
        UnitRole = Role.Ranger;
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
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
            case State.KnockBack:
                UpdateKnockBack();
                break;
            case State.Divide:
                UpdateDivide();
                break;
            case State.Faint:
                break;
            case State.Standby:
                break;
        }
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        
        if (_rush && Rushed == false)
        {
            MoveSpeed += RushSpeed;
            State = State.Rush; 
        }
        else
        {
            State = State.Moving;
        }
    }

    protected override void UpdateRush()
    {
        if (Room == null) return;
        
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            MoveSpeed -= RushSpeed;
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
        if (distance <= /*Stat.SizeX * 0.25 +*/ 1f)
        {
            ApplyRollEffect(Target);
            Mp += MpRecovery;
            State = State.KnockBack;
            
            double radians = Math.PI * Dir / 180;
            Vector3 dirVector = new((float)Math.Sin(radians), 0, (float)Math.Cos(radians));
            DestPos = CellPos + dirVector * 3;
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected override void UpdateKnockBack()
    {
        if (Room == null) return;
        
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        if (distance <= 0.4f)
        {
            Rushed = true;
            MoveSpeed -= RushSpeed;
            State = State.Idle;
            return;
        }
        
        (Path, Atan) = Room.Map.KnockBack(this, Dir);
        BroadcastPath();
    }

    private void UpdateDivide()
    {
        var destPacket = new S_SetKnockBack
        {
            Dest = new DestVector { X = DestPos.X, Y = DestPos.Y - BounceParam, Z = DestPos.Z },
            ObjectId = Id
        };
        Room?.Broadcast(destPacket);
    }

    public void OnDivide()
    {
        Vector3 dividePos;
        do
        {
            Random random = new();
            double minRadius = 2;
            double maxRadius = 3;
            double radius = minRadius + (maxRadius - minRadius) * random.NextDouble();

            double directionRadian = Dir * (Math.PI / 180);
            double minTheta = directionRadian;
            double maxTheta = directionRadian + Math.PI;

            double theta = random.NextDouble() * (maxTheta - minTheta) + minTheta;
            float x = (float)(radius * Math.Cos(theta));
            float z = (float)(radius * Math.Sin(theta));

            dividePos = new Vector3(CellPos.X + x, CellPos.Y, CellPos.Z + z);
        } while (Room!.Map.CanGo(this, Room.Map.Vector3To2(dividePos)) == false);

        DestPos = dividePos;
        CellPos = DestPos;
        Room.Map.ApplyMap(this, CellPos);
        DivideEvents(_divideDuration);
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;            Room.SpawnProjectile(_poison ?
                ProjectileId.SmallPoison : ProjectileId.BasicProjectile, this, 5f);            
        });
    }

    private async void DivideEvents(long impactTime)
    {
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Room == null) return;
            State = State.Idle;
        });
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0 || AddBuffAction == null) return;
        
        if (_poison)
        {
            Room.Push(AddBuffAction, BuffId.Addicted,
                BuffParamType.Percentage, target, this, 0.05f, 5000, false);
        }
        else if (_nestedPoison)
        {
            Room.Push(AddBuffAction, BuffId.Addicted,
                BuffParamType.Percentage, target, this, 0.05f, 5000, true);
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
    }
    
    protected virtual void ApplyRollEffect(GameObject? target)
    {
        if (target == null || Room == null) return;
        
        if (_rollDamageUp)
        {
            Room.Push(target.OnDamaged, this, TotalSkillDamage * 2, Damage.Normal, false);
        }
        else
        {
            Room.Push(target.OnDamaged, this, TotalSkillDamage, Damage.Normal, false);
        }
    }
    
    protected override void OnDead(GameObject? attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
        if (Room == null) return;
        
        Targetable = false;
        if (attacker != null)
        {
            attacker.KillLog = Id;
            if (attacker.Target != null)
            {
                if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
                {
                    if (attacker.Parent != null) attacker.Parent.Target = null;
                }
                attacker.Target = null;
            }
        }
        
        if (AlreadyRevived == false && WillRevive)
        {
            if (AttackEnded == false) AttackEnded = true;  
            Room.Broadcast(new S_Die { ObjectId = Id, Revive = true});
            return;
        }

        if (Degeneration)
        {
            Room.Map.ApplyLeave(this);

            var lurkerPos = new PositionInfo
            {
                Dir = Dir, PosX = PosInfo.PosX, PosY = PosInfo.PosY, PosZ = PosInfo.PosZ
            };
            Room.SpawnMonster(UnitId.Lurker, lurkerPos, Player);
            Room.LeaveGame(Id);
            return;
        }

        Room.Broadcast(new S_Die { ObjectId = Id });
        Room.DieAndLeave(Id);
    }
}
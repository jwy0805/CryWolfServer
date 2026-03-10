using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Resources;
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
    
    protected readonly float RushSpeed = DataManager.SkillDict[(int)Skill.CreeperRoll].Value;
    protected readonly float BounceParam = 2f;
    protected readonly float PoisonValue = 0.05f;
    
    protected int RushDamage => (int)(TotalSkillDamage * DataManager.SkillDict[(int)Skill.CreeperRoll].Coefficient);
    protected int UpgradedRushDamage => (int)(RushDamage * DataManager.SkillDict[(int)Skill.CreeperRollDamageUp].Coefficient);
    
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
        if (Room == null) return;
        
        if (_rush && !Rushed)
        {
            int attackType = (int)Data.AttackType.Ground;
            float rushRange = SizeZ / (float)Room.Map.CellCnt;
            if (Room.TryPickTargetAndPath(
                    this, attackType, rushRange, Path, out var target, true))
            {
                Target = target;
                if (Target == null || !Target.Targetable || Target.Room != Room) return;
            }
            
            MoveSpeed += RushSpeed;
            State = State.Rush; 
        }
        else
        {
            if (Room.TryPickTargetAndPath(
                    this, AttackType, TotalAttackRange, Path, out var target, true))
            {
                Target = target;
                if (Target == null || !Target.Targetable || Target.Room != Room) return;
            }
            
            State = State.Moving;
        }
    }

    protected override void UpdateRush()
    {
        if (Room == null) return;
        
        int attackType = (int)Data.AttackType.Ground;
        float rushRange = SizeZ / (float)Room.Map.CellCnt;
        if (Room.TryPickTargetAndPath(
                this, attackType, rushRange, Path, out GameObject? target, true))
        {
            Target = target;
        }
        
        if (Target == null || !Target.Targetable || Target.Room != Room)
        {  
            MoveSpeed -= RushSpeed;
            State = State.Idle;
            return;
        }

        if (Path.Count <= 1)
        {
            DestPos = Room.Map.GetClosestPoint(this, Target);
            
            double dx = DestPos.X - CellPos.X;
            double dz = DestPos.Z - CellPos.Z;
            
            Dir = (float)Math.Round(Math.Atan2(dx, dz) * (180 / Math.PI), 2);
            State = State.KnockBack;
            
            SyncPosAndDir();
            ApplyRollEffect(Target);
            
            double radians = Math.PI * Dir / 180;
            Vector3 dirVector = new((float)Math.Sin(radians), 0, (float)Math.Cos(radians));
            
            DestPos = Util.Util.NearestCell(CellPos - dirVector * 3);
            Mp += MpRecovery;

            return;
        }
        
        // Target이 있으면 이동
        Room.Map.MoveAlongPath(this, Path, Atan);
        BroadcastPath();
    }
    
    protected override void UpdateKnockBack()
    {
        if (Room == null) return;

        if (!Room.Map.TryKnockBack(this, Path))
        {
            State = State.Idle;
            SyncPosAndDir();
            return;        
        }
        
        if (Path.Count <= 1)
        {
            Rushed = true;
            MoveSpeed -= RushSpeed;
            State = State.Idle;
            SyncPosAndDir();
            return;
        }
        
        Room.Map.MoveAlongPath(this, Path, Atan);
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
        } while (!Room!.Map.CanGo(this, Room.Map.Vector3To2(dividePos)));

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
            if (State == State.Faint) return;            
            Room.SpawnProjectile(_poison ? ProjectileId.SmallPoison : ProjectileId.BasicProjectile, this, 5);            
        });
    }

    private async void DivideEvents(long impactTime)
    {
        try
        {
            await Scheduler.ScheduleEvent(impactTime, () =>
            {
                if (Room == null) return;
                State = State.Idle;
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error in DivideEvents: {e.Message}");
        }
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0 || AddBuffAction == null) return;
        
        if (_poison)
        {
            Room.Push(AddBuffAction, BuffId.Addicted,
                BuffParamType.Percentage, target, this, PoisonValue, 5000, false);
        }
        else if (_nestedPoison)
        {
            Room.Push(AddBuffAction, BuffId.Addicted,
                BuffParamType.Percentage, target, this, PoisonValue, 5000, true);
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
    }
    
    protected virtual void ApplyRollEffect(GameObject? target)
    {
        if (target == null || Room == null) return;
        
        if (_rollDamageUp && target.ObjectType == GameObjectType.Fence)
        {
            Room.Push(target.OnDamaged, this, UpgradedRushDamage, Damage.Normal, false);
        }
        else
        {
            Room.Push(target.OnDamaged, this, RushDamage, Damage.Normal, false);
        }
    }
    
    protected override void OnDead(GameObject? attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
        if (Room == null) return;
        
        Targetable = false;
        State = State.Die;
        Room.RemoveAllBuffs(this);
        
        if (attacker != null)
        {
            attacker.KillLog = Id;
            attacker.Target = null;
            
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile && attacker.Parent != null)
            {
                attacker.Parent.Target = null;
            }
        }
        
        if (!AlreadyRevived && WillRevive)
        {
            if (!AttackEnded) AttackEnded = true;  
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
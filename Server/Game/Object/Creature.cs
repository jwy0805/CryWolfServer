using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Creature : GameObject
{
    protected virtual Skill NewSkill { get; set; }
    protected Skill Skill;
    protected readonly List<Skill> SkillList = new();
    protected long DeltaTime;
    protected float AttackSpeedReciprocal;
    protected float SkillSpeedReciprocal;
    protected float SkillSpeedReciprocal2;
    protected const long MpTime = 1000;

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
            }   
        }
    }
    
    protected virtual void UpdateIdle() { }
    protected virtual void UpdateMoving() { }
    protected virtual void UpdateAttack() { }
    protected virtual void UpdateSkill() { }
    protected virtual void UpdateSkill2() { }
    protected virtual void UpdateKnockBack() { }
    protected virtual void UpdateRush() { }
    protected virtual void UpdateDie() { }
    public virtual void SkillInit() { }
    public virtual void RunSkill() { }

    public virtual void SetNormalAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
    
    public virtual void SetAdditionalAttackEffect(GameObject target) { }
    public virtual void SetEffectEffect() { }

    public virtual void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
    public virtual void SetAdditionalProjectileEffect(GameObject target) { }

    public virtual void SetNextState()
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

    public virtual void SetNextState(State state)
    {
        
    }
    
    protected virtual void SetDirection()
    {
        if (Room == null) return;
        if (Target == null)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }

        if (Target.Stat.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        BroadcastPos();
    }
    
    protected virtual Vector3 GetRandomDestInFence()
    {
        List<Vector3> sheepBound = GameData.SheepBounds;
        float minX = sheepBound.Select(v => v.X).ToList().Min() + 1.0f;
        float maxX = sheepBound.Select(v => v.X).ToList().Max() - 1.0f;
        float minZ = sheepBound.Select(v => v.Z).ToList().Min() + 1.0f;
        float maxZ = sheepBound.Select(v => v.Z).ToList().Max() - 1.0f;

        do
        {
            Random random = new();
            Map map = Room!.Map;
            float x = Math.Clamp((float)random.NextDouble() * (maxX - minX) + minX, minX, maxX);
            float z = Math.Clamp((float)random.NextDouble() * (maxZ - minZ) + minZ, minZ, maxZ);
            Vector3 dest = Util.Util.NearestCell(new Vector3(x, 6.0f, z));
            bool canGo = map.CanGo(this, map.Vector3To2(dest));
            if (canGo) return dest;
        } while (true);
    }
}
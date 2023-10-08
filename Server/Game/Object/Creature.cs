using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Creature : GameObject
{
    protected virtual Skill NewSkill { get; set; }
    protected Skill Skill;
    protected List<Skill> SkillList = new();
    public List<BuffManager.IBuff> BuffList = new();

    protected long DeltaTime;
    protected const long MpTime = 1000;

    public override void Update()
    {
        base.Update();
        if (Room == null) return;
        // if (ObjectType == GameObjectType.Tower) Console.WriteLine(State.ToString());
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room!.Stopwatch.ElapsedMilliseconds;
            Mp += Stat.MpRecovery;
        }

        if (MaxMp != 1 && Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastMove();
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
    protected virtual void SkillInit() { }
    public virtual void RunSkill() { }

    public virtual void SetNormalAttackEffect(GameObject master) { }
    
    public virtual void SetNextState()
    {
        if (Room == null) return;

        if (Target == null || Target.Stat.Targetable == false)
        {
            State = State.Idle;
        }
        else
        {
            if (Target.Hp > 0)
            {
                Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));
                if (distance <= AttackRange)
                {
                    State = State.Attack;
                    SetDirection();
                }
                else
                {
                    DestPos = Target.CellPos;
                    (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                    State = State.Moving;
                }
            }
            else
            {
                Target = null;
                State = State.Idle;
            }
        }
        
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }
    
    protected virtual void SetDirection()
    {
        if (Room == null) return;
        if (Target == null)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }

        if (Target.Stat.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        BroadcastMove();
    }
    
    protected virtual Vector3 GetRandomDestInFence()
    {
        int level = Room!.StorageLevel;
        List<Vector3> sheepBound = GameData.SheepBounds[level];
        float minX = sheepBound.Select(v => v.X).ToList().Min();
        float maxX = sheepBound.Select(v => v.X).ToList().Max();
        float minZ = sheepBound.Select(v => v.Z).ToList().Min();
        float maxZ = sheepBound.Select(v => v.Z).ToList().Max();

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
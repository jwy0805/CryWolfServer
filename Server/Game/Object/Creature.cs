using System.Numerics;
using Google.Protobuf.Protocol;
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
        if (Room!.Stopwatch.ElapsedMilliseconds > Time + MpTime)
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
                State = distance <= AttackRange ? State.Attack : State.Moving;
            }
            else
            {
                Target = null;
                State = State.Idle;
            }
        }
        
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }
}
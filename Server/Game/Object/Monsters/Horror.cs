using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Horror : Creeper
{
    private bool _poisonStack = false;
    private bool _rollPoison = false;
    private bool _poisonBelt = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HorrorRollPoison:
                    _rollPoison = true;
                    break;
                case Skill.HorrorPoisonStack:
                    _poisonStack = true;
                    Room?.Broadcast(new S_SkillUpdate
                    {
                        ObjectEnumId = (int)MonsterId,
                        ObjectType = GameObjectType.Monster,
                        SkillType = SkillType.SkillProjectile
                    });
                    break;
                case Skill.HorrorHealth:
                    MaxHp += 200;
                    Hp += 200;
                    BroadcastHealth();
                    break;
                case Skill.HorrorDefence:
                    Defence += 5;
                    TotalDefence += 5;
                    break;
                case Skill.HorrorPoisonResist:
                    PoisonResist += 15;
                    TotalPoisonResist += 15;
                    break;
                case Skill.HorrorPoisonBelt:
                    _poisonBelt = true;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += Stat.MpRecovery;
        }

        if (Mp >= MaxMp && _poisonBelt)
        {
            Effect poisonBelt = ObjectManager.Instance.CreateEffect(EffectId.PoisonBelt);
            poisonBelt.Room = Room;
            poisonBelt.Parent = this;
            poisonBelt.PosInfo = PosInfo;
            poisonBelt.Info.PosInfo = Info.PosInfo;
            poisonBelt.Info.Name = EffectId.PoisonBelt.ToString();
            poisonBelt.Init();
            Room.EnterGame_Parent(poisonBelt, this);
            Mp = 0;
        }
        
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

    protected override void UpdateRush()
    {
        // Targeting
        double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
        if (timeNow > LastSearch + SearchTick)
        {
            LastSearch = timeNow;
            GameObject? target = Room?.FindNearestTarget(this);
            if (Target?.Id != target?.Id)
            {
                Target = target;
                if (Target != null)
                {
                    DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                    (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                }
            }
        }
        
        if (Target == null || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }
        
        if (Room != null)
        {
            // 이동
            // target이랑 너무 가까운 경우
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                // Roll 충돌 처리
                if (distance <= Stat.SizeX * 0.25 + 0.75f)
                {
                    CellPos = position;
                    Target.OnDamaged(this, SkillDamage);
                    if (_rollPoison)
                    {
                        BuffManager.Instance.AddBuff(_poisonStack ? BuffId.DeadlyAddicted : BuffId.Addicted,
                            Target, this, Attack);
                    }
                    Mp += MpRecovery;
                    State = State.KnockBack;
                    DestPos = CellPos + -Vector3.Normalize(Target.CellPos - CellPos) * 3;
                    BroadcastMove();
                    Room.Broadcast(new S_SetKnockBack
                    {
                        ObjectId = Id, 
                        Dest = new DestVector { X = DestPos.X, Y = DestPos.Y, Z = DestPos.Z }
                    });
                    return;
                }
            }
        }

        BroadcastMove();
    }
}
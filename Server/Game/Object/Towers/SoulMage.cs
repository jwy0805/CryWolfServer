using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SoulMage : Haunt
{
    public bool Fire = false;
    private bool _tornado = false;
    private bool _shareDamage = false;
    private bool _natureAttack = false;
    private bool _debuffResist = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            // switch (Skill)
            // {
            //     case Skill.SoulMageAvoid:
            //         Evasion += 20;
            //         break;
            //     case Skill.SoulMageDefenceAll:
            //         Defence += 20;
            //         FireResist += 35;
            //         PoisonResist += 35;
            //         break;
            //     case Skill.SoulMageFireDamage:
            //         Fire = true;
            //         break;
            //     case Skill.SoulMageTornado:
            //         _tornado = true;
            //         break;
            //     case Skill.SoulMageShareDamage:
            //         _shareDamage = true;
            //         break;
            //     case Skill.SoulMageNatureAttack:
            //         _natureAttack = true;
            //         break;
            //     case Skill.SoulMageDebuffResist:
            //         _debuffResist = true;
            //         break;
            //     case Skill.SoulMageCritical:
            //         CriticalChance += 25;
            //         CriticalMultiplier = 1.5f;
            //         break;
            // }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime * 5 && _natureAttack)
        {
            Time = Room!.Stopwatch.ElapsedMilliseconds;
            List<GameObjectType> typeList = new() { GameObjectType.Monster };
            List<Creature> monsters = Room.FindTargets(this, typeList, AttackRange, 2).Cast<Creature>().ToList();
            if (monsters.Any())
            {
                Creature monster = monsters.OrderBy(_ => Guid.NewGuid()).ToList().First();
                Effect greenGate = ObjectManager.Instance.CreateEffect(EffectId.GreenGate);
                greenGate.Room = Room;
                greenGate.Parent = this;
                greenGate.Target = monster;
                greenGate.PosInfo = monster.PosInfo;
                greenGate.Info.PosInfo = monster.Info.PosInfo;
                greenGate.Info.Name = nameof(EffectId.GreenGate);
                greenGate.Init();
                Room.EnterGameTarget(greenGate, greenGate.Parent, monster);
            }
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
    
    protected override void UpdateMoving()
    {
        // Targeting
        Target = Room?.FindClosestTarget(this);
        if (Target != null)
        {
            DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
            (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos, false);
            BroadcastDest();
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
            // Attack
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                if (distance <= AttackRange)
                {
                    CellPos = position;
                    State = _tornado ? State.Skill : State.Attack;
                    BroadcastMove();
                    return;
                }
            }
            
            BroadcastMove();
        }
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        
        int totalDamage = attacker.CriticalChance > 0 
            ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
            : Math.Max(damage - TotalDefence, 0);
        
        if (_shareDamage)
        {
            // List<UnitId> towerIds = new() 
            //     { ((Tower)this).UnitId.PracticeDummy, ((Tower)this).UnitId.TargetDummy, ((Tower)this).UnitId.TrainingDummy }; 
            // GameObject? nearestDummy = Room.FindNearestTower(towerIds);
            // if (nearestDummy == null)
            // {
            //     Hp = Math.Max(Stat.Hp - damage, 0);
            // }
            // else
            // {
            //     damage = (int)(damage * 0.5f);
            //     nearestDummy.OnDamaged(attacker, damage);
            //     Hp = Math.Max(Stat.Hp - damage, 0);
            // }
        }
        else
        {
            Hp = Math.Max(Stat.Hp - totalDamage, 0);
        }
        
        if (Reflection && reflected == false)
        {
            int refParam = (int)(damage * ReflectionRate);
            attacker.OnDamaged(this, refParam, damageType, true);
        }
        
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp <= 0) OnDead(attacker);
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        if (_debuffResist == false || !Buffs.Any()) return;
        BuffId buffId = Buffs.OrderBy(_ => Guid.NewGuid()).ToList().First();
        
        Buffs.Remove(buffId);
    }
    
    public override void SetNextState()
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
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
                if (distance <= AttackRange)
                {
                    State = _tornado ? State.Skill : State.Attack;
                    SetDirection();
                }
                else
                {
                    State = State.Idle;
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
}
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Horror : Creeper
{
    private bool _poisonImmunity = false;
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
                case Skill.HorrorPoisonBelt:
                    _poisonBelt = true;
                    break;
                case Skill.HorrorPoisonImmunity:
                    _poisonImmunity = true;
                    break;
                case Skill.HorrorRollPoison:
                    _rollPoison = true;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

        if (Mp >= MaxMp && _poisonBelt)
        {
            Effect poisonBelt = ObjectManager.Instance.CreateEffect(EffectId.PoisonBelt);
            poisonBelt.Room = Room;
            poisonBelt.Parent = this;
            poisonBelt.PosInfo = PosInfo;
            poisonBelt.Info.PosInfo = Info.PosInfo;
            poisonBelt.Info.Name = EffectId.PoisonBelt.ToString();
            poisonBelt.Init();
            Room.EnterGameParent(poisonBelt, this);
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
        if (Start == false)
        {
            Start = true;
            MoveSpeedParam += 2;
            BroadcastDest();
            State = State.Rush;
            BroadcastPos();
            return;
        }
        
        if (Start && SpeedRestore == false)
        {
            MoveSpeedParam -= 2;
            SpeedRestore = true;
            BroadcastDest();
        }
        
        base.UpdateMoving();
    }
    
    protected override void UpdateRush()
    {
        if (Target == null || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }
        
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
                Target.OnDamaged(this, SkillDamage, Damage.Normal);
                if (_rollPoison)
                {   // RollPoison Effect
                    var effect = Room.EnterEffect(EffectId.HorrorRoll, this);
                    Room.EnterGameParent(effect, effect.Parent ?? this);
                    BuffManager.Instance.AddBuff(BuffId.DeadlyAddicted, Target, this, 0.05f, 5);
                }
                Mp += MpRecovery;
                State = State.KnockBack;
                DestPos = CellPos + -Vector3.Normalize(Target.CellPos - CellPos) * 3;
                BroadcastPos();
                Room.Broadcast(new S_SetKnockBack
                {
                    ObjectId = Id, 
                    Dest = new DestVector { X = DestPos.X, Y = DestPos.Y, Z = DestPos.Z }
                });
                return;
            }
        }

        BroadcastPos();
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        if (damageType is Damage.Poison && _poisonImmunity) return;

        int totalDamage;
        if (damageType is Damage.Normal or Damage.Magical)
        {
            totalDamage = attacker.CriticalChance > 0 
                ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
                : Math.Max(damage - TotalDefence, 0);
            if (damageType is Damage.Normal && Reflection && reflected == false)
            {
                int refParam = (int)(totalDamage * ReflectionRate);
                attacker.OnDamaged(this, refParam, damageType, true);
            }
        }
        else
        {
            totalDamage = damage;
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp <= 0) OnDead(attacker);
    }
}
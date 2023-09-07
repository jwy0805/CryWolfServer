using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class GameObject : IGameObject
{
    public Player Player;
    
    protected const int CallCycle = 200;
    protected long Time;
    protected const int SearchTick = 600;
    protected double LastSearch = 0;

    public List<Vector3> Path = new();
    public List<Vector3> Dest = new();
    public List<double> Atan = new();
    public GameObject? Target;
    public GameObject? Parent;
    public Vector3 DestPos;
    private float _totalAttackSpeed;

    public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
    public int Id
    {
        get => Info.ObjectId;
        set => Info.ObjectId = value;
    }

    public GameRoom? Room { get; set; }
    public ObjectInfo Info { get; set; } = new();
    public PositionInfo PosInfo { get; set; } = new();
    public StatInfo Stat { get; private set; } = new();
    public int TotalAttack;

    public float TotalAttackSpeed
    {
        get => _totalAttackSpeed;
        set
        { 
            _totalAttackSpeed = value;
            Room?.Broadcast(new S_SetAnimSpeed()
            {
                ObjectId = Id,
                Param = _totalAttackSpeed
            });
        }
    }

    public int TotalDefence;
    public float TotalMoveSpeed;
    public int TotalAccuracy;
    public int TotalEvasion;
    public int TotalFireResist;
    public int TotalPoisonResist;

    #region Stat
    
    public virtual int Hp
    {
        get => Stat.Hp;
        set => Stat.Hp = Math.Clamp(value, 0, Stat.MaxHp);
    }

    public int MaxHp
    {
        get => Stat.MaxHp;
        set => Stat.MaxHp = value;
    }

    public int Mp
    {
        get => Stat.Mp;
        set => Stat.Mp = Math.Clamp(value, 0, Stat.MaxMp);
    }

    public int MaxMp
    {
        get => Stat.MaxMp;
        set => Stat.MaxMp = value;
    }

    public int MpRecovery
    {
        get => Stat.MpRecovery;
        set => Stat.MpRecovery = value;
    }

    public int Attack
    {
        get => Stat.Attack;
        protected set
        {
            TotalAttack -= Stat.Attack;
            Stat.Attack = value;
            TotalAttack += Stat.Attack;
        }
    }

    public int SkillDamage
    {
        get => Stat.Skill;
        set => Stat.Skill = value;
    }

    public int Defence
    {
        get => Stat.Defence;
        protected set
        {
            TotalDefence -= Stat.Defence;
            Stat.Defence = value;
            TotalDefence += Stat.Defence;
        }    
    }

    public int FireResist
    {
        get => Stat.FireResist;
        protected set
        {
            TotalFireResist -= Stat.FireResist;
            Stat.FireResist = value;
            TotalFireResist += Stat.FireResist;
        }
    }

    public int PoisonResist
    {
        get => Stat.PoisonResist;
        protected set
        {
            TotalPoisonResist -= Stat.PoisonResist;
            Stat.PoisonResist = value;
            TotalPoisonResist += Stat.PoisonResist;
        }
    }

    public float MoveSpeed
    {
        get => Stat.MoveSpeed;
        protected set
        {
            TotalMoveSpeed -= Stat.MoveSpeed;
            Stat.MoveSpeed = value;
            TotalMoveSpeed += Stat.MoveSpeed;
        }
    }

    public float AttackSpeed
    {
        get => Stat.AttackSpeed;
        protected set
        {
            TotalAttackSpeed -= Stat.AttackSpeed;
            Stat.AttackSpeed = value;
            TotalAttackSpeed += Stat.AttackSpeed;
        }
    }

    public float AttackRange
    {
        get => Stat.AttackRange;
        set => Stat.AttackRange = value;
    }

    public int CriticalChance
    {
        get => Stat.CriticalChance;
        set => Stat.CriticalChance = value;
    }

    public float CriticalMultiplier
    {
        get => Stat.CriticalMultiplier;
        set => Stat.CriticalMultiplier = value;
    }

    public int Accuracy
    {
        get => Stat.Accuracy;
        protected set
        {
            TotalAccuracy -= Stat.Accuracy;
            Stat.Accuracy = value;
            TotalAccuracy += Stat.Accuracy;
        }
    }

    public int Evasion
    {
        get => Stat.Evasion;
        protected set
        {
            TotalEvasion -= Stat.Evasion;
            Stat.Evasion = value;
            TotalEvasion += Stat.Evasion;
        }
    }

    public bool Targetable
    {
        get => Stat.Targetable;
        set => Stat.Targetable = value;
    }

    public bool Aggro
    {
        get => Stat.Aggro;
        set => Stat.Aggro = value;
    }

    public bool Reflection
    {
        get => Stat.Reflection;
        set => Stat.Reflection = value;
    }

    public bool ReflectionSkill
    {
        get => Stat.ReflectionSkill;
        set => Stat.ReflectionSkill = value;
    }

    public float ReflectionRate
    {
        get => Stat.ReflectionRate;
        set => Stat.ReflectionRate = value;
    }

    public int UnitType
    {
        get => Stat.UnitType;
        set => Stat.UnitType = value;
    }

    public int AttackType
    {
        get => Stat.AttackType;
        set => Stat.AttackType = value;
    }
    
    #endregion
    
    public State State
    {
        get => PosInfo.State;
        set => PosInfo.State = value;
    }

    public float Dir
    {
        get => PosInfo.Dir;
        set => PosInfo.Dir = value;
    }
    
    public GameObject()
    {
        Info.PosInfo = PosInfo;
        Info.StatInfo = Stat;
    }
    
    public Vector3 CellPos
    {
        get => new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
        set
        {
            PosInfo.PosX = value.X;
            PosInfo.PosY = value.Y;
            PosInfo.PosZ = value.Z;
        }
    }

    private IJob _job;

    public virtual void Update()
    {
        if (Room != null) _job = Room.PushAfter(CallCycle, Update);
    }

    public virtual void Init()
    {
        Time = Room!.Stopwatch.ElapsedMilliseconds;
    }
    
    public void StatInit()
    {
        TotalAttack = Attack;
        TotalAttackSpeed = AttackSpeed;
        TotalDefence = Defence;
        TotalMoveSpeed = MoveSpeed;
        TotalAccuracy = Accuracy;
        TotalEvasion = Evasion;
        TotalFireResist = FireResist;
        TotalPoisonResist = PoisonResist;
    }

    
    public virtual void OnDamaged(GameObject attacker, int damage)
    {
        if (Room == null) return;
        damage = Math.Max(damage - TotalDefence, 0);
        Hp = Math.Max(Stat.Hp - damage, 0);

        S_ChangeHp hpPacket = new S_ChangeHp { ObjectId = Id, Hp = Hp };
        Room.Broadcast(hpPacket);
        if (Hp <= 0) OnDead(attacker);
    }

    public virtual void OnDead(GameObject attacker)
    {
        if (Room == null) return;
        if (attacker.Target != null) attacker.Target.Targetable = false;
        
        S_Die diePacket = new S_Die { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);

        GameRoom room = Room;
        room.LeaveGame(Id);
    }
    
    protected virtual void BroadcastMove()
    {
        S_Move movePacket = new() { ObjectId = Id, PosInfo = PosInfo };
        // Console.WriteLine($"{movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosZ}");
        Room?.Broadcast(movePacket);
    }

    public virtual void BroadcastDest()
    {
        if (Dest.Count == 0 || Atan.Count == 0) return;
        S_SetDest destPacket = new S_SetDest { ObjectId = Id , MoveSpeed = TotalMoveSpeed };
        
        for (int i = 0; i < Dest.Count; i++)
        {
            DestVector destVector = new DestVector { X = Dest[i].X, Y = Dest[i].Y, Z = Dest[i].Z };
            destPacket.Dest.Add(destVector);
            destPacket.Dir.Add(Atan[i]);
        }
        Room?.Broadcast(destPacket);
    }

    public virtual void ApplyMap(Vector3 posInfo)
    {
        PosInfo.PosX = posInfo.X;
        PosInfo.PosY = posInfo.Y;
        PosInfo.PosZ = posInfo.Z;
        bool canGo = Room!.Map.ApplyMap(this);
        if (!canGo) State = State.Idle;
    }
}
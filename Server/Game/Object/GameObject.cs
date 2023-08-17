using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class GameObject : IGameObject
{
    public Player Player;
    
    protected const int CallCycle = 200;
    protected const int SearchTick = 600;
    protected double LastSearch = 0;

    protected List<Vector3> Path = new();
    protected List<double> Atan = new();
    public GameObject? Target;
    public GameObject? Parent;
    protected Vector3 DestPos;

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
    public float TotalAttackSpeed;
    public int TotalDefence;
    public float TotalMoveSpeed;
    public int TotalAccuracy;
    public int TotalEvasion;
    public int TotalFireResist;
    public int TotalPoisonResist;

    #region Stat
    
    public int Hp
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
        set => Stat.Mp = Math.Clamp(value, 0, Stat.Mp);
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
        set => Stat.Attack = value;
    }

    public int SkillDamage
    {
        get => Stat.Skill;
        set => Stat.Skill = value;
    }

    public int Defence
    {
        get => Stat.Defence;
        set => Stat.Defence = value;
    }

    public int FireResist
    {
        get => Stat.FireResist;
        set => Stat.FireResist = value;
    }

    public int PoisonResist
    {
        get => Stat.PoisonResist;
        set => Stat.PoisonResist = value;
    }

    public float MoveSpeed
    {
        get => Stat.MoveSpeed;
        set => Stat.MoveSpeed = value;
    }

    public float AttackSpeed
    {
        get => Stat.AttackSpeed;
        set => Stat.AttackSpeed = value;
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
        set => Stat.Accuracy = value;
    }

    public int Evasion
    {
        get => Stat.Evasion;
        set => Stat.Evasion = value;
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
        StatInit();
    }
    
    private void StatInit()
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
        if (Path.Count == 0 || Atan.Count == 0) return;
        S_SetDest destPacket = new S_SetDest { ObjectId = Id , MoveSpeed = TotalMoveSpeed };
        
        for (int i = 0; i < Path.Count; i++)
        {
            DestVector destVector = new DestVector { X = Path[i].X, Y = Path[i].Y, Z = Path[i].Z };
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
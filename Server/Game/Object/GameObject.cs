using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class GameObject
{
    public Player Player = new();
    
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
    public virtual int TotalAttack => Stat.Attack;
    public virtual int TotalSkill => Stat.Attack; 
    public virtual int TotalDefence => Stat.Defence;

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

    public int Hp
    {
        get => Stat.Hp;
        set => Stat.Hp = Math.Clamp(value, 0, Stat.MaxHp);
    }
    
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

    protected IJob Job;
    public virtual void Update() { }
    
    public virtual void OnDamaged(GameObject attacker, int damage)
    {
        if (Room == null) return;
        damage = Math.Max(damage - TotalDefence, 0);
        Stat.Hp = Math.Max(Stat.Hp - damage, 0);

        S_ChangeHp hpPacket = new S_ChangeHp { ObjectId = Id, Hp = Stat.Hp };
        Room.Broadcast(hpPacket);
        if (Stat.Hp <= 0) OnDead(attacker);
    }

    public virtual void OnDead(GameObject attacker)
    {
        if (Room == null) return;
        if (attacker.Target != null) attacker.Target.Stat.Targetable = false;
        
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
        S_SetDest destPacket = new S_SetDest { ObjectId = Id };
        
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
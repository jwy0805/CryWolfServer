using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class GameObject
{
    protected List<Vector3> Path = new();
    protected List<double> Atan = new();
    
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
    
    public virtual void Update() { }

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

    public virtual void OnDamaged(GameObject attacker, int damage)
    {
        if (Room == null) return;
        damage = Math.Max(damage - TotalDefence, 0);
        
    }

    public virtual void OnDead(GameObject attacker)
    {
        if (Room == null) return;
    }
    
    protected virtual void BroadcastMove()
    {
        S_Move movePacket = new() { ObjectId = Id, PosInfo = PosInfo };
        Console.WriteLine($"{movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosZ}");
        Room?.Broadcast(movePacket);
    }

    public virtual void BroadcastDest()
    {
        if (Path.Count == 0 || Atan.Count == 0) return;
        S_SetDest destPacket = new() { ObjectId = Id };
        DestVector destVector = new DestVector();
        
        for (int i = 0; i < Path.Count; i++)
        {
            destVector.X = Path[i].X;
            destVector.Y = Path[i].Y;
            destVector.Z = Path[i].Z;
            destPacket.Dest.Add(destVector);
            destPacket.Dir.Add(Atan[i]);
        }
        Room?.Broadcast(destPacket);
    }
}
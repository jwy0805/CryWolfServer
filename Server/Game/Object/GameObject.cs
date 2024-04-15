using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public partial class GameObject : IGameObject
{
    public Player Player;
    
    protected const int CallCycle = 200;
    protected long Time;
    protected int SearchTick = 500;
    protected double LastSearch = 0;
    protected Vector3 DestPos;

    public List<Vector3> Path = new();
    protected List<Vector3> Dest = new();
    protected List<double> Atan = new();
    
    public int Id
    {
        get => Info.ObjectId;
        set => Info.ObjectId = value;
    }
    public List<BuffId> Buffs { get; set; } = new();
    public GameObject? Target { get; set; }
    public GameObject? Parent { get; set; }
    public SpawnWay Way { get; set; }
    public bool Burn { get; set; }
    public bool Invincible { get; set; }
    public GameRoom? Room { get; set; }
    public ObjectInfo Info { get; set; } = new();
    public PositionInfo PosInfo { get; set; } = new();
    public StatInfo Stat { get; private set; } = new();
    public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
    
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
    
    public virtual void Init()
    {
        if (Room == null) return;
        Time = Room.Stopwatch.ElapsedMilliseconds;
    }
    
    protected IJob Job;
    public virtual void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
    }
    
    public void StatInit()
    {
        Hp = MaxHp;
    }
    
    public virtual void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        
        int totalDamage = attacker.CriticalChance > 0 
            ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
            : Math.Max(damage - TotalDefence, 0);
        Hp = Math.Max(Stat.Hp - damage, 0);
        
        if (Reflection && reflected == false)
        {
            int refParam = (int)(totalDamage * ReflectionRate);
            attacker.OnDamaged(this, refParam, damageType, true);
        }

        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp <= 0) OnDead(attacker);
    }

    public virtual void OnDead(GameObject attacker)
    {
        if (Room == null) return;
        Targetable = false;
        if (attacker.Target != null)
        {
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
            {
                if (attacker.Parent != null)
                {
                    attacker.Parent.Target = null;
                    attacker.State = State.Idle;
                    BroadcastMove();
                }
            }
            attacker.Target = null;
            attacker.State = State.Idle;
            BroadcastMove();
        }
        
        S_Die diePacket = new() { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
        // Room.LeaveGame(Id);
    }
    
    public virtual void BroadcastMove()
    {
        S_Move movePacket = new() { ObjectId = Id, PosInfo = PosInfo };
        Room?.Broadcast(movePacket);
    }

    public virtual void BroadcastDest()
    {
        if (Dest.Count == 0 || Atan.Count == 0) return;
        S_SetDest destPacket = new S_SetDest { ObjectId = Id , MoveSpeed = MoveSpeed };
        
        for (int i = 0; i < Dest.Count; i++)
        {
            DestVector destVector = new DestVector { X = Dest[i].X, Y = Dest[i].Y, Z = Dest[i].Z };
            destPacket.Dest.Add(destVector);
        }

        for (int i = 0; i < Atan.Count; i++)
        {
            if (Atan.Count != 0) destPacket.Dir.Add(Atan[i]);
        }
        
        Room?.Broadcast(destPacket);
    }

    public virtual void BroadcastHealth()
    {
        S_ChangeMaxHp maxHpPacket = new S_ChangeMaxHp { ObjectId = Id, MaxHp = MaxHp };
        S_ChangeHp hpPacket = new S_ChangeHp { ObjectId = Id, Hp = Hp };
        Room?.Broadcast(maxHpPacket);
        Room?.Broadcast(hpPacket);
    }

    public virtual void ApplyMap(Vector3 posInfo)
    {
        if (Room == null) return;
        bool canGo = Room.Map.ApplyMap(this, posInfo);
        if (!canGo) State = State.Idle;
        BroadcastMove();
    }
}
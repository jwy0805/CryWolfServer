using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public partial class GameObject : IGameObject
{
    protected long Time;
    protected int SearchTick = 500;
    protected double LastSearch = 0;
    protected List<Vector3> Path = new();
    protected List<double> Atan = new();

    public int CallCycle => 200;
    public int DistRemainder { get; set; } = 0;
    public bool WillRevive { get; set; } = false;
    public bool AlreadyRevived { get; set; } = false;
    public float ReviveHpRate { get; set; } = 0.3f;
    
    public virtual int KillLog { get; set; }
    
    public int Id
    {
        get => Info.ObjectId;
        set => Info.ObjectId = value;
    }
    public Player Player { get; set; }
    public List<BuffId> Buffs { get; set; } = new();
    public GameObject? Target { get; set; }
    public GameObject? Parent { get; set; }
    public SpawnWay Way { get; set; }
    public bool Burn { get; set; }
    public bool Invincible { get; set; }
    public GameRoom? Room { get; set; }
    public ObjectInfo Info { get; set; } = new();
    public PositionInfo PosInfo { get; set; } = new();
    public Vector3 DestPos { get; set; }
    public StatInfo Stat { get; private set; } = new();
    public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
    
    public virtual State State
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
        get => new(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
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

    public virtual void OnDead(GameObject attacker)
    {
        if (Room == null) return;
        attacker.KillLog = Id;
        Targetable = false;
        if (attacker.Target != null)
        {
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
            {
                if (attacker.Parent != null)
                {
                    attacker.Parent.Target = null;
                    attacker.State = State.Idle;
                    // BroadcastPos();
                }
            }
            attacker.Target = null;
            attacker.State = State.Idle;
            // BroadcastPos();
        }
        
        if (AlreadyRevived == false && WillRevive)
        {
            S_Die dieAndRevivePacket = new() { ObjectId = Id, AttackerId = attacker.Id, Revive = true};
            Room.Broadcast(dieAndRevivePacket);
            return;
        }

        S_Die diePacket = new() { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
    }
    
    public virtual void BroadcastPos()
    {
        S_Move movePacket = new() { ObjectId = Id, PosInfo = PosInfo };
        Room?.Broadcast(movePacket);
    }

    public virtual void BroadcastState()
    {
        Room?.Broadcast(new S_State { ObjectId = Id, State = State});
    }

    protected virtual void BroadcastPath()
    {
        if (Path.Count == 0 || Path.Count != Atan.Count) return;
        
        var pathPacket = new S_SetPath { ObjectId = Id , MoveSpeed = TotalMoveSpeed };
        for (var i = 0; i < Path.Count; i++)
        {
            pathPacket.Path.Add(new DestVector { X = Path[i].X, Y = Path[i].Y, Z = Path[i].Z });
            pathPacket.Dir.Add(Atan[i]);
        }
        
        Room?.Broadcast(pathPacket);
    }

    protected virtual void BroadcastProjectilePath()
    {   // TODO: Unit Size에 맞춰서 Y 조정
        if (Path.Count == 0) return;
        
        var pathPacket = new S_SetProjectilePath { ObjectId = Id, MoveSpeed = MoveSpeed, Dir = Dir };
        for (var i = 0; i < Path.Count; i++)
        {
            pathPacket.Path.Add(new DestVector { X = Path[i].X, Y = Path[i].Y + 1, Z = Path[i].Z });
        }
        
        Room?.Broadcast(pathPacket);
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
        BroadcastPos();
    }
}
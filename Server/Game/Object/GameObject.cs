using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public partial class GameObject : IGameObject
{
    protected long Time;
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
    
    public virtual void OnDamaged(GameObject? attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        if (new Random().Next(100) < TotalEvasion)
        {
            // TODO: Evasion Effect
            return;
        }
        
        var totalDamage = damageType is Damage.Normal or Damage.Magical 
            ? Math.Max(damage - TotalDefence, 0) : damage;
        if (damageType is Damage.Normal && Reflection && reflected == false)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            attacker?.OnDamaged(this, reflectionDamage, damageType, true);
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp <= 0) OnDead(attacker);
    }
    
    protected virtual void OnDead(GameObject? attacker)
    {
        if (Room == null) return;
        
        Targetable = false;
        if (attacker != null)
        {
            attacker.KillLog = Id;
            if (attacker.Target != null)
            {
                if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
                {
                    if (attacker.Parent != null) attacker.Parent.Target = null;
                }
                attacker.Target = null;
            }
        }
        
        if (AlreadyRevived == false && WillRevive)
        {
            S_Die dieAndRevivePacket = new() { ObjectId = Id, Revive = true};
            Room.Broadcast(dieAndRevivePacket);
            return;
        }

        S_Die diePacket = new() { ObjectId = Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
    }
    
    public virtual void BroadcastPos()
    {
        S_Move movePacket = new() { ObjectId = Id, PosInfo = PosInfo };
        Room?.Broadcast(movePacket);
    }

    protected virtual void BroadcastState()
    {
        Room?.Broadcast(new S_State { ObjectId = Id, State = State});
    }

    protected virtual void BroadcastPath()
    {
        if (Path.Count == 0) return;
        var pathPacket = new S_SetPath { ObjectId = Id , MoveSpeed = TotalMoveSpeed };
        for (var i = 0; i < Path.Count; i++)
        {
            pathPacket.Path.Add(new DestVector { X = Path[i].X, Y = Path[i].Y, Z = Path[i].Z });
            pathPacket.Dir.Add(Path.Count == Atan.Count ? Atan[i] : Dir);
        }
        
        Room?.Broadcast(pathPacket);
    }   
    
    public virtual void BroadcastHp() 
    {
        var hpPacket = new S_ChangeHp { ObjectId = Id, Hp = Hp, MaxHp = MaxHp};
        Room?.Broadcast(hpPacket);
    }

    public virtual void BroadcastMp()
    {
        var mpPacket = new S_ChangeMp { ObjectId = Id, Mp = Mp, MaxMp = MaxMp};
    }
}
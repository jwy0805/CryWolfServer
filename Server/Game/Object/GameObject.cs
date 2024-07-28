using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public partial class GameObject : IGameObject
{
    protected long Time;
    protected IJob? Job;
    protected List<Vector3> Path = new();
    protected List<double> Atan = new();

    public int CallCycle => 200;
    public int DistRemainder { get; set; }
    public bool WillRevive { get; set; }
    public bool AlreadyRevived { get; protected set; }
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
    public GameObjectType ObjectType { get; protected init; } = GameObjectType.None;
    
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
    
    public virtual void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
    }
    
    protected void StatInit()
    {
        Hp = MaxHp;
    }
    
    public virtual void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null)
        {
            Console.WriteLine($"Room is null for GameObject with Id {Id}");
            return;
        }
        
        if (Invincible || Targetable == false || Hp <= 0) return;
        var random = new Random();
        
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {   // Evasion
            // TODO: Evasion Effect
            return;
        }
        
        // 일반적으로 Normal Damage 만 Critical 가능, Magical이나 True Damage Critical 구현 시 데미지를 넣는 Unit으로부터 자체적으로 계산
        var totalDamage = random.Next(100) < attacker.CriticalChance && damageType is Damage.Normal
            ? (int)(damage * attacker.CriticalMultiplier) : damage;
        
        if (ShieldRemain > 0)
        {   // Shield
            ShieldRemain -= totalDamage;
            if (ShieldRemain >= 0) return;
            totalDamage = Math.Abs(ShieldRemain);
            ShieldRemain = 0;
        }

        totalDamage = damageType is Damage.Normal or Damage.Magical
            ? Math.Max(totalDamage - TotalDefence, 0) : damage;
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        
        if (Hp <= 0)
        {   // Dead
            OnDead(attacker);
            return;
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false && attacker.Targetable)
        {   // Reflection
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            Room.Push(attacker.OnDamaged, this, reflectionDamage, damageType, true);
        }
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
            var dieAndRevivePacket = new S_Die { ObjectId = Id, Revive = true };
            Room.Broadcast(dieAndRevivePacket);
            return;
        }

        var diePacket = new S_Die { ObjectId = Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
    }

    public virtual void AddBuff(Buff buff)
    {
        if (Room == null) return;
        if (Invincible && buff.Type is BuffType.Debuff) return;
        Buffs.Add(buff.Id);
        Room.Buffs.Add(buff);
        buff.TriggerBuff();
    }
    
    public virtual void BroadcastPos()
    {
        Room?.Broadcast(new S_Move { ObjectId = Id, PosInfo = PosInfo });
    }

    public virtual void BroadcastMoveForward()
    {
        Room?.Broadcast(new S_MoveForwardObject
        {
            ObjectId = Id, Dest = new DestVector { X = CellPos.X, Y = CellPos.Y, Z = CellPos.Z }
        });
    }

    protected virtual void BroadcastState()
    {
        Room?.Broadcast(new S_State { ObjectId = Id, State = State });
    }

    protected virtual void BroadcastPath()
    {   // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Path == null || Path.Count == 0) return;
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
        Room?.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp, MaxHp = MaxHp});
    }

    public virtual void BroadcastShield()
    {
        Room?.Broadcast(new S_ChangeShield { ObjectId = Id, ShieldRemain = ShieldRemain, ShieldAdd = ShieldAdd});
    }
    
    public virtual void BroadcastMp()
    {
        Room?.Broadcast(new S_ChangeMp { ObjectId = Id, Mp = Mp, MaxMp = MaxMp});
    }
}
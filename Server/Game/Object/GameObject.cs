using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class GameObject : IGameObject
{
    public Player Player;
    
    protected const int CallCycle = 200;
    protected long Time;
    protected int SearchTick = 500;
    protected double LastSearch = 0;

    public List<Vector3> Path = new();
    protected List<Vector3> Dest = new();
    protected List<double> Atan = new();
    public readonly List<BuffId> Buffs = new();
    public GameObject? Target;
    public GameObject? Parent;
    protected Vector3 DestPos;
    public SpawnWay Way;
    private float _totalAttackSpeed;

    public bool Burn { get; set; }
    public bool Invincible { get; set; }
    
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

    public int TotalSkill;
    public int TotalDefence;
    public float TotalMoveSpeed;
    public int TotalAccuracy;
    public int TotalEvasion;
    public int TotalFireResist;
    public int TotalPoisonResist;

    #region Stat
    
    public Behaviour Behaviour { get; set; }
    
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

    public float SkillRange
    {
        get => Stat.SkillRange;
        set => Stat.SkillRange = value;
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
    
    public int Resource
    {
        get => Stat.Resource;
        set => Stat.Resource = value;
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
    
    public virtual void Init()
    {
        if (Room == null) return;
        Time = Room.Stopwatch.ElapsedMilliseconds;
        StatInit();
    }
    
    protected IJob Job;
    public virtual void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
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
    
    public virtual void OnDamaged(GameObject attacker, int damage, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        
        damage = attacker.CriticalChance > 0 
            ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
            : Math.Max(damage - TotalDefence, 0);
        Hp = Math.Max(Stat.Hp - damage, 0);
        
        if (Reflection && reflected == false)
        {
            int refParam = (int)(damage * ReflectionRate);
            attacker.OnDamaged(this, refParam, true);
        }

        S_ChangeHp hpPacket = new S_ChangeHp { ObjectId = Id, Hp = Hp };
        Room.Broadcast(hpPacket);
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
                    attacker.Parent.Target = null;
            }
            attacker.Target = null;
        }
        
        S_Die diePacket = new S_Die { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);

        GameRoom room = Room;
        room.LeaveGame(Id);
    }
    
    public virtual void BroadcastMove()
    {
        S_Move movePacket = new() { ObjectId = Id, PosInfo = PosInfo };
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
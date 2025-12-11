using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Resources;
using Server.Util;

namespace Server.Game;

public class Sheep : Creature, ISkillObserver
{
    private bool _idle = false;
    private long _idleTime;
    private readonly float _infectionDist = 3f;
    
    protected float SheepBoundMargin = 2.0f;
    protected int YieldTime = 6000;
    protected float YieldInterval => YieldTime / 20000f;

    public SheepId SheepId { get; set; }
    public int YieldIncrement { get; set; }
    public int YieldDecrement { get; set; }
    public bool YieldStop { get; set; }
    public bool Infection { get; set; }
    
    public Sheep()
    {
        ObjectType = GameObjectType.Sheep;
    }
    
    public override void Init()
    {
        base.Init();
        DataManager.ObjectDict.TryGetValue((int)SheepId ,out var objectData);
        Stat.MergeFrom(objectData!.Stat);
        Hp = objectData.Stat.MaxHp;
        
        State = State.Idle;
        if (Room == null) return;
        YieldTime = Room.GameData.RoundTime / 4;
        Time = Room.Stopwatch.ElapsedMilliseconds + YieldTime;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        Yield();
        
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
            case State.KnockBack:
                UpdateKnockBack();
                break;
            case State.Faint:
                break;
            case State.Standby:
                break;
        }   
    }
    
    protected override void UpdateIdle()
    {
        if (_idle == false)
        {
            _idleTime = Room!.Stopwatch.ElapsedMilliseconds;
            _idle = true;
        }
        
        if (Room?.Stopwatch.ElapsedMilliseconds > _idleTime + new Random().Next(1000, 2500))
        {
            DestPos = GetRandomDestInFence();
            State = State.Moving;
            BroadcastPos();
            _idle = false;
        }
    }

    protected override void UpdateMoving()
    {
        if (Room == null || AddBuffAction == null) return;
        
        if (Infection)
        {   // 모기 공격 맞았을 때 독 감염시키는 메서드
            var sheeps = Room!.FindTargets(this,
                new List<GameObjectType> { GameObjectType.Sheep }, _infectionDist);
            foreach (var sheep in sheeps.Select(s => s as Creature))
            {
                if (sheep != null)
                {
                    Room.Push(AddBuffAction, BuffId.Addicted,
                        BuffParamType.Percentage, sheep, this, 0.05f, 5000, false);
                }
            }
        }
        
        // 이동
        Vector3 position = CellPos;
        float distance = Vector3.Distance(DestPos, CellPos);
        if (distance <= 0.5f)
        {
            CellPos = position;
            State = State.Idle;
            BroadcastPos();
        }
        
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected virtual void Yield()
    {
        if (Room == null) return;
        if (Room.Stopwatch.ElapsedMilliseconds <= Time + YieldTime || State == State.Die || Room.Round == 0) return;
        var param = (int)((Room.GameInfo.TotalSheepYield + YieldIncrement - YieldDecrement) * YieldInterval);
        Time = Room.Stopwatch.ElapsedMilliseconds;
        Room.YieldCoin(this, Math.Clamp(param, 0, param));
    }
    
    public override void OnSkillUpgrade(Skill skill)
    {
        string skillName = skill.ToString();
        string sheepName = "Sheep";
        if (skillName.Contains(sheepName))
        {
            NewSkill = skill;
            SkillList.Add(NewSkill);
        }
    }
    
    protected override void SkillInit()
    {
        var skillUpgradedList = Player.SkillUpgradedList;
        var sheepName = "Sheep";
        if (skillUpgradedList.Count == 0) return;
        
        foreach (var skill in skillUpgradedList)
        {
            var skillName = skill.ToString();
            if (skillName.Contains(sheepName)) SkillList.Add(skill);
        }

        if (SkillList.Count != 0)
        {
            foreach (var skill in SkillList) NewSkill = skill;
        }
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        base.OnDamaged(attacker, damage, damageType, reflected);
        
        if (Room == null) return;
        Room.GameInfo.SheepDamageThisRound += damage;
    }
    
    protected override void OnDead(GameObject? attacker)
    {
        // Tutorial -> Sheep never dies when attacked by NPC Wolf
        if (Room is { GameMode: GameMode.Tutorial } && Room.FindPlayer(go =>
                go is Player { IsNpc: true, Faction: Faction.Wolf }) != null)
        {
            Hp = MaxHp;
            Room.RemoveAllMonsters();
            return;
        }

        if (Room != null)
        {
            var monster = attacker as Monster ?? attacker?.Parent as Monster;
            if (monster != null && attacker != null)
            {
                Room?.YieldDna(this, attacker);
            }
        
            Room!.GameInfo.SheepCount--;
            Room.GameInfo.TheNumberOfDestroyedSheep++;
            Room.GameInfo.SheepDeathsThisRound++;
            base.OnDead(attacker);
        }
    }
    
    protected virtual Vector3 GetRandomDestInFence()
    {
        if (Room == null) return new Vector3();
        
        Vector3[] sheepBound = Room.GetSheepBounds();
        float minX = sheepBound.Select(v => v.X).ToList().Min() + SheepBoundMargin;
        float maxX = sheepBound.Select(v => v.X).ToList().Max() - SheepBoundMargin;
        float minZ = sheepBound.Select(v => v.Z).ToList().Min() + SheepBoundMargin;
        float maxZ = sheepBound.Select(v => v.Z).ToList().Max() - SheepBoundMargin;

        Random random = new();
        do
        {
            Map map = Room.Map;
            float x = Math.Clamp((float)random.NextDouble() * (maxX - minX) + minX, minX, maxX);
            float z = Math.Clamp((float)random.NextDouble() * (maxZ - minZ) + minZ, minZ, maxZ);
            Vector3 dest = Util.Util.NearestCell(new Vector3(x, 6.0f, z));
            bool canGo = map.CanGo(this, map.Vector3To2(dest));
            float dist = Vector3.Distance(CellPos, dest);
            if (canGo && dist > 1.5f) return dest;
        } while (true);
    }
}
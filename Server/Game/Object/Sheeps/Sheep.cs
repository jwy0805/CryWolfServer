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
        if (!_idle)
        {
            _idleTime = Room!.Stopwatch.ElapsedMilliseconds;
            _idle = true;
        }
        
        if (Room?.Stopwatch.ElapsedMilliseconds > _idleTime + Random.Shared.Next(1000, 2500))
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
        {   
            // 모기 공격 맞았을 때 독 감염시키는 메서드
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
        
        Path.Clear();
        Atan.Clear();

        var startCell = Room.Map.Vector3To2(CellPos);
        var destCell  = Room.Map.Vector3To2(DestPos);

        if (!Room.Map.TryFindPath(this, startCell, destCell, Path, 0, false, checkObjects: true))
        {
            State = State.Idle;
            SyncPosAndDir();
            return;
        }

        Room.Map.RemoveDuplicatedPaths(Path);

        if (Path.Count <= 1)
        {
            State = State.Idle;
            SyncPosAndDir();
            return;
        }

        Room.Map.MoveAlongPath(this, Path, Atan);
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
    
    protected virtual Vector3 GetRandomDestInFence(float radius = 5f)
    {
        if (Room == null) return default;

        Vector3[] b = Room.GetSheepBounds();

        // bounds 계산
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

        for (int i = 0; i < b.Length; i++)
        {
            float x = b[i].X;
            float z = b[i].Z;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        minX += SheepBoundMargin;
        maxX -= SheepBoundMargin;
        minZ += SheepBoundMargin;
        maxZ -= SheepBoundMargin;

        // bounds가 너무 좁아지면 현재 위치 유지
        if (minX >= maxX || minZ >= maxZ) return CellPos;

        Map map = Room.Map;
        var random = Random.Shared;

        // 현재 위치 기준
        Vector3 origin = CellPos;

        const int maxTry = 64; 
        float r2Min = 1.5f * 1.5f; // 너무 가까운 건 배제
        float rad = radius;

        for (int t = 0; t < maxTry; t++)
        {
            // 원 내부 균일 샘플링
            double u = random.NextDouble();
            double v = random.NextDouble();
            double rr = Math.Sqrt(u) * rad;
            double theta = v * (Math.PI * 2.0);

            float dx = (float)(rr * Math.Cos(theta));
            float dz = (float)(rr * Math.Sin(theta));
            float x = origin.X + dx;
            float z = origin.Z + dz;

            if (x < minX || x > maxX || z < minZ || z > maxZ) continue;

            Vector3 dest = Util.Util.NearestCell(new Vector3(x, 6.0f, z));

            // 너무 가까운 경우 버림 (월드 거리 기준)
            Vector3 flatO = origin; flatO.Y = 0;
            Vector3 flatD = dest;   flatD.Y = 0;
            float distSq = Vector3.DistanceSquared(flatO, flatD);
            if (distSq <= r2Min) continue;
            if (!map.CanGo(this, map.Vector3To2(dest), checkObjects: true)) continue;

            return dest;
        }

        return CellPos;
    }
}
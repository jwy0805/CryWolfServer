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
    private readonly long _yieldTime = 5000;
    
    protected float SheepBoundMargin = 1.0f;

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
        Stat.MergeFrom(objectData!.stat);
        Hp = objectData.stat.MaxHp;
        
        State = State.Idle;
        if (Room == null) return;
        Time = Room.Stopwatch.ElapsedMilliseconds + _yieldTime;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (Room.Stopwatch.ElapsedMilliseconds > Time + _yieldTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            YieldCoin(Room.GameInfo.SheepYield + YieldIncrement - YieldDecrement);
        }
        
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

    public void YieldCoin(int yield)
    {
        if (Room == null) return;
        Resource resource;
        
        switch (yield)
        {
            case < 50:
                resource = ObjectManager.Instance.Create<Resource>(ResourceId.CoinStarSilver);
                break;
            case < 100:
                resource = ObjectManager.Instance.Create<Resource>(ResourceId.CoinStarGolden);
                break;
            case < 200:
                resource = ObjectManager.Instance.Create<Resource>(ResourceId.PouchGreen);
                break;
            case < 300:
                resource = ObjectManager.Instance.Create<Resource>(ResourceId.PouchRed);
                break;
            default:
                resource = ObjectManager.Instance.Create<Resource>(ResourceId.ChestGold);
                break;
        }

        resource.Yield = yield;
        resource.CellPos = CellPos + new Vector3(0, 0.5f, 0);
        resource.Player = Player;
        resource.Init();
        GameObject go = resource;
        Room.Push(Room.EnterGame, go);
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

    protected override void OnDead(GameObject? attacker)
    {
        Room!.GameInfo.SheepCount--;
        base.OnDead(attacker);
    }
    
    protected virtual Vector3 GetRandomDestInFence()
    {
        if (Room == null) return new Vector3();
        
        Vector3[] sheepBound = Room.GetSheepBounds();
        float minX = sheepBound.Select(v => v.X).ToList().Min() + SheepBoundMargin;
        float maxX = sheepBound.Select(v => v.X).ToList().Max() - SheepBoundMargin;
        float minZ = sheepBound.Select(v => v.Z).ToList().Min() + 1;
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
            if (canGo && dist > 3f) return dest;
        } while (true);
    }
}
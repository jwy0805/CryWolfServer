using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Resources;

namespace Server.Game;

public class Sheep : Creature, ISkillObserver
{
    private readonly int _sheepNo = 1;
    private long _lastSetDest = 0;
    private bool _idle = false;
    private long _idleTime;
    private long _lastYieldTime = 0;
    private readonly float _infectionDist = 3f;

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
        DataManager.ObjectDict.TryGetValue(_sheepNo ,out var objectData);
        Stat.MergeFrom(objectData!.stat);
        Hp = objectData.stat.MaxHp;
        
        State = State.Idle;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room?.Stopwatch.ElapsedMilliseconds > _lastYieldTime + GameData.RoundTime)
        {
            if (YieldStop == false)
            {
                Resource = GameData.SheepYield;
                _lastYieldTime = Room.Stopwatch.ElapsedMilliseconds;
                YieldCoin(Resource + YieldIncrement - YieldDecrement);
                YieldIncrement = 0;
                YieldDecrement = 0;
            }

            YieldStop = false;
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
            _idleTime = Room.Stopwatch.ElapsedMilliseconds;
            _idle = true;
        }
        
        if (Room?.Stopwatch.ElapsedMilliseconds > _idleTime + new Random().Next(1000, 2500))
        {
            DestPos = GetRandomDestInFence();
            (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            State = State.Moving;
            BroadcastMove();
            _idle = false;
        }
    }

    protected override void UpdateMoving()
    {
        if (!Infection) return;
        
        List<GameObject> sheeps = Room.FindBuffTargets(this,
            new List<GameObjectType> { GameObjectType.Sheep }, _infectionDist);
        foreach (var sheep in sheeps.Select(s => s as Creature))
        {
            if (sheep != null) BuffManager.Instance.AddBuff(BuffId.Addicted, sheep, this, 0.05f);
        }
    }

    public void OnSkillUpgrade(Skill skill)
    {
        string skillName = skill.ToString();
        string sheepName = "Sheep";
        if (skillName.Contains(sheepName))
        {
            NewSkill = skill;
            SkillList.Add(NewSkill);
        }
    }

    private void YieldCoin(int yield)
    {
        if (Room == null) return;
        Resource resource;
        
        switch (yield)
        {
            case < 30:
                resource = ObjectManager.Instance.CreateResource(ResourceId.CoinStarSilver);
                break;
            case < 100:
                resource = ObjectManager.Instance.CreateResource(ResourceId.CoinStarGolden);
                break;
            case < 200:
                resource = ObjectManager.Instance.CreateResource(ResourceId.PouchGreen);
                break;
            case < 300:
                resource = ObjectManager.Instance.CreateResource(ResourceId.PouchRed);
                break;
            default:
                resource = ObjectManager.Instance.CreateResource(ResourceId.ChestGold);
                break;
        }

        resource.Yield = yield;
        resource.CellPos = CellPos + new Vector3(0, 0.5f, 0);
        resource.Player = Player;
        resource.Init();
        GameObject go = resource;
        Room.Push(Room.EnterGame, go);
    }
    
    protected override void SkillInit()
    {
        List<Skill> skillUpgradedList = Player.SkillUpgradedList;
        string sheepName = "Sheep";
        if (skillUpgradedList.Count == 0) return;
        
        foreach (var skill in skillUpgradedList)
        {
            string skillName = skill.ToString();
            if (skillName.Contains(sheepName)) SkillList.Add(skill);
        }

        if (SkillList.Count != 0)
        {
            foreach (var skill in SkillList) NewSkill = skill;
        }
    }
}
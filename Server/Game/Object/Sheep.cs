using System.Diagnostics;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Sheep : Creature, ISkillObserver
{
    private readonly int _sheepNo = 1;
    private long _lastSetDest = 0;
    
    private long _lastYieldTime = 0;
    private int _yieldDecrease = 0;
    private int _yieldInterrupt = 0;
    private float _decreaseParam = 0;
    private int _interruptParam = 0;
    private bool _decreased = false;
    private bool _interrupted = false;
    private bool _infection = false;

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
        if (Room != null) Job = Room.PushAfter(CallCycle, Update);
        
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
        if (Room?.Stopwatch.ElapsedMilliseconds > _lastSetDest + new Random().Next(1000, 2500))
        {
            _lastSetDest = Room.Stopwatch.ElapsedMilliseconds;
            DestPos = GetRandomDestInFence();
            (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            State = State.Moving;
        }
    }

    protected override void UpdateMoving()
    {
        if (Room != null)
        {
            // 이동
            double deltaX = DestPos.X - CellPos.X;
            double deltaZ = DestPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);

            BroadcastMove();

            if (Room?.Stopwatch.ElapsedMilliseconds > _lastYieldTime + GameData.RoundTime)
            {
                _lastYieldTime = Room.Stopwatch.ElapsedMilliseconds;
                YieldCoin(GameData.SheepYield);
            }
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
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
        }
    }

    private Vector3 GetRandomDestInFence()
    {
        int level = Room!.StorageLevel;
        List<Vector3> sheepBound = GameData.SheepBounds[level];
        float minX = sheepBound.Select(v => v.X).ToList().Min();
        float maxX = sheepBound.Select(v => v.X).ToList().Max();
        float minZ = sheepBound.Select(v => v.Z).ToList().Min();
        float maxZ = sheepBound.Select(v => v.Z).ToList().Max();

        do
        {
            Random random = new();
            Map map = Room!.Map;
            float x = Math.Clamp((float)random.NextDouble() * (maxX - minX) + minX, minX, maxX);
            float z = Math.Clamp((float)random.NextDouble() * (maxZ - minZ) + minZ, minZ, maxZ);
            Vector3 dest = Util.Util.NearestCell(new Vector3(x, 6.0f, z));
            bool canGo = map.CanGo(this, map.Vector3To2(dest));
            if (canGo) return dest;
        } while (true);
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
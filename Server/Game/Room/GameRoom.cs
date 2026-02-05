using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Data.SinglePlayScenario;
using Server.Game.AI;
using Server.Game.Enchants;
using Server.Game.Resources;

namespace Server.Game;

public partial class GameRoom : JobSerializer, IDisposable
{
    private readonly object _lock = new();
    
    private readonly Dictionary<int, Player> _players = new();
    private readonly Dictionary<int, Tower> _towers = new();
    private readonly Dictionary<int, Monster> _monsters = new();
    private readonly Dictionary<int, MonsterStatue> _statues = new();
    private readonly Dictionary<int, Sheep> _sheeps = new();
    private readonly Dictionary<int, Fence> _fences = new();
    private readonly Dictionary<int, Effect> _effects = new();
    private readonly Dictionary<int, Projectile> _projectiles = new();

    private readonly UpkeepTracker<Tower> _towerTracker;
    private readonly UpkeepTracker<MonsterStatue> _statueTracker;
    private bool _notifiedTowerExcess;
    private bool _notifiedStatueExcess;
    
    private GameMode _gameMode;
    private readonly Random _random = new();
    private int _round;
    private readonly long _interval = 1000;
    private long _timeSendTime;
    private Storage? _storage;
    private Portal? _portal;
    private bool _infoInit;
    private bool _gameOver;
    private Stage? _tutorialWaveModule;
    private ITutorialTrigger _tutorialTrigger = new IgnoreTutorialTrigger();
    
    private readonly Dictionary<Faction, AiController> _aiControllers = new();
    
    public GameMode GameMode
    {
        get => _gameMode;
        set
        {
            _gameMode = value;

            _tutorialTrigger = _gameMode switch
            {
                GameMode.Tutorial => new TutorialTriggerService(_gameMode),
                _ => new IgnoreTutorialTrigger()
            };
        } 
    }
    public readonly Stopwatch Stopwatch = new();
    public HashSet<Buff> Buffs { get; } = new();
    public Enchant? Enchant { get; set; }
    public GameInfo GameInfo { get; private set; } = new(new Dictionary<int, Player>(), 1);
    public List<UnitSize> UnitSizeList { get; set; } = new();
    public int Round => _round;
    public int RoomId { get; set; }
    public int RoundTime { get; private set; } = 24;
    public bool RoomActivated { get; set; }
    public Map Map { get; private set; } = new();
    public int MapId { get; set; }
    public GameManager.GameData GameData { get; set; } = new();
    
    // Single Play Properties
    public int StageId { get; set; }
    // Tutorial Properties
    public bool TutorialSpawnFlag { get; set; }
    
    public struct TargetDistance
    {
        public GameObject Target;
        public float Distance;
    }
    
    public int GetBaseLevel(Faction faction) => faction == Faction.Sheep ? GetStorageLevel() : GetPortalLevel();
    private int GetStorageLevel() => _storage?.Level ?? 0;
    private int GetPortalLevel() => _portal?.Level ?? 0;
    public int GetNumberOfTowers() => _towers.Count;
    public int GetNumberOfStatues() => _statues.Count;

    public GameRoom()
    {
        _towerTracker = new UpkeepTracker<Tower>(tower => tower.UnitId, this, Faction.Sheep);
        _statueTracker = new UpkeepTracker<MonsterStatue>(statue => statue.UnitId, this, Faction.Wolf);
    }
    
    public void Init(int mapId)
    {
        GameData = GameManager.Instance.GameDataCache[mapId];
        Map.GameData = GameData;
        MapId = mapId;
        Map.LoadMap(mapId);
        Map.MapSetting();
        Map.Room = this;
        Console.WriteLine("Room initialized.");
        UnitSizeMapping();
        GameInit();
        InitAi();
    }

    private void UnitSizeMapping()
    {
        int[] unitIds = Enum.GetValues(typeof(UnitId)) as int[] ?? Array.Empty<int>();
        
        foreach (var unitId in unitIds)
        {
            DataManager.UnitDict.TryGetValue(unitId, out var unitData);
            var stat = new StatInfo();
            stat.MergeFrom(unitData?.Stat);
            UnitSizeList.Add(new UnitSize((UnitId)unitId, stat.SizeX, stat.SizeZ));
        }
    }
    
    public void Update()
    {
        ManageTutorial();
        if (RoomActivated == false)
        {
            return;
        }

        Flush();
        SetTimeAndRound();
        UpdateBuffs();
    } 
     
    private void SetTimeAndRound()
    {
        long time = Stopwatch.ElapsedMilliseconds;
        if (time < _timeSendTime + _interval || time < 1000 || _gameOver) return;
        _ = CheckRegular();
        
        Broadcast(new S_Time { Time = RoundTime, Round = _round});
        RoundTime--;
        
        // --- Single Play ---
        UpdateAiModule();
        
        if (RoundTime < 0) 
        {
            InitRound();
        }
        
        if (RoundTime < 10 && _round != 0)
        {
            CheckMonsters();
        }
        // --- Single Play ---
        
        _timeSendTime = time;
    }

    private void UpdateAiModule()
    {
        if (GameMode != GameMode.Single && GameMode != GameMode.AiSimulation) return;
        if (RoundTime >= 23) return;

        var snapshot = BuildWorldSnapshot();
        
        // AI logging
        if (_aiControllers.TryGetValue(Faction.Sheep, out var aiSheep))
        {
            var blackboardSheep = BuildBlackboard(snapshot, Faction.Sheep, aiSheep.Policy);
            aiSheep.Update(this, blackboardSheep);
            // Console.WriteLine("--- AI Sheep Update End ---");
        }

        if (_aiControllers.TryGetValue(Faction.Wolf, out var aiWolf))
        {
            var blackboardWolf = BuildBlackboard(snapshot, Faction.Wolf, aiWolf.Policy);
            aiWolf.Update(this, blackboardWolf);
            // Console.WriteLine("--- AI Wolf Update End ---");
        }
    }
    
    public void EnterGameNpc(Player npc)
    {
        _players.TryAdd(npc.Id, npc);
        Console.WriteLine($"NPC Player {npc.Id} {npc.Faction} entered.");
        npc.Room = this;
        npc.IsNpc = true;
        npc.Init();
    }
    
    public void EnterGame(GameObject gameObject)
    {
        var type = ObjectManager.GetObjectTypeById(gameObject.Id);
        
        switch (type)
        {
            case GameObjectType.Player:
                var player = (Player)gameObject;
                _players.TryAdd(player.Id, player);
                Console.WriteLine($"Player {player.Id} entered.");
                player.Room = this;
                player.Init();
                // 본인에게 정보 전송
                var enterPacket = new S_EnterGame { Player = player.Info };
                player.Session?.Send(enterPacket);
                {
                    var spawnPacket = new S_Spawn(); 
                    foreach (var p in _players.Values.Where(p => player != p)) 
                        spawnPacket.Objects.Add(p.Info);
                    // 게임 플레이 중간에 player가 접속했을 때 이미 존재하는 objects spawn
                    foreach (var f in _fences.Values) spawnPacket.Objects.Add(f.Info);
                    foreach (var m in _monsters.Values) spawnPacket.Objects.Add(m.Info);
                    foreach (var s in _sheeps.Values) spawnPacket.Objects.Add(s.Info);
                    foreach (var ms in _statues.Values) spawnPacket.Objects.Add(ms.Info);
                    foreach (var t in _towers.Values) spawnPacket.Objects.Add(t.Info);
                    foreach (var e in _effects.Values) spawnPacket.Objects.Add(e.Info);
                    spawnPacket.Objects.Add(_portal?.Info);
                    spawnPacket.Objects.Add(_storage?.Info);
                    player.Session?.Send(spawnPacket);
                }
                break;
            
            case GameObjectType.Tower:
                var tower = (Tower)gameObject;
                if (tower.UnitType == 1) gameObject.PosInfo.PosY += 2;
                gameObject.Info.Name = tower.UnitId.ToString();
                gameObject.Info.PosInfo = gameObject.PosInfo;
                tower.Info = gameObject.Info;
                _towers.TryAdd(gameObject.Id, tower);
                Map.ApplyMap(tower);
                tower.Update();
                break;
            
            case GameObjectType.Monster:
                var monster = (Monster)gameObject;
                if (monster.UnitType == 1) gameObject.PosInfo.PosY += 2;
                gameObject.Info.Name = monster.UnitId.ToString();
                gameObject.PosInfo.Dir = 180;
                gameObject.Info.PosInfo = gameObject.PosInfo;
                monster.Info = gameObject.Info;
                _monsters.TryAdd(gameObject.Id, monster);
                Map.ApplyMap(monster);
                monster.Update();
                break;
            
            case GameObjectType.MonsterStatue:
                var statue = (MonsterStatue)gameObject;
                var monsterName = statue.UnitId.ToString();
                gameObject.Info.Name = string.Concat(monsterName, "Statue");
                statue.Info = gameObject.Info;
                _statues.TryAdd(gameObject.Id, statue);
                Map.ApplyMap(statue);
                statue.Update();
                break;
            
            case GameObjectType.Fence:
                var fence = (Fence)gameObject;
                fence.Info = gameObject.Info;
                _fences.TryAdd(gameObject.Id, fence);
                fence.Room = this;
                Map.ApplyMap(fence);
                break;
            
            case GameObjectType.Sheep:
                var sheep = (Sheep)gameObject;
                gameObject.PosInfo.State = State.Idle;
                gameObject.Info.Name = sheep.SheepId.ToString();
                sheep.Info = gameObject.Info;
                _sheeps.TryAdd(gameObject.Id, sheep);
                sheep.Room = this;
                Map.ApplyMap(sheep);
                sheep.Update();
                break;
            
            case GameObjectType.Resource:
                var resource = (Resource)gameObject;
                resource.Info.Name = resource.ResourceId.ToString();
                resource.Room = this;
                resource.Player = gameObject.Player;
                resource.Info = gameObject.Info;
                resource.Init();
                resource.Update();
                break;
            
            case GameObjectType.Portal:
                var portal = (Portal)gameObject;
                portal.Room = this;
                _portal = portal;
                Map.ApplyMap(_portal); 
                break;
            
            case GameObjectType.Storage:
                var storage = (Storage)gameObject;
                storage.Room = this;
                _storage = storage;
                _storage.Init();
                break;
        }
        
        // 타인에게 정보 전송
        var spawnPacketToBroadcast = new S_Spawn();
        spawnPacketToBroadcast.Objects.Add(gameObject.Info);
        foreach (var player in _players.Values.Where(player => player.Id != gameObject.Id)) 
        {
            player.Session?.Send(spawnPacketToBroadcast);
        }
    }

    private void EnterGameProjectile(
        GameObject gameObject, Vector3 targetPos, float speed, int parentId, bool sound = true) 
    {
        var projectile = (Projectile)gameObject;
        _projectiles.TryAdd(projectile.Id, projectile);
        projectile.Room = this;
        var destVector = new DestVector { X = targetPos.X, Y = targetPos.Y + 0.5f, Z = targetPos.Z };
        var spawnPacket = new S_SpawnProjectile
        { 
            Object = gameObject.Info, ParentId = parentId, DestPos = destVector, MoveSpeed = speed, Sound = sound
        };
        Broadcast(spawnPacket);
    }

    private void EnterGameEffect(
        GameObject gameObject, int parentId, bool trailingParent, int duration = 2000, float scale = 1)
    {
        var effect = (Effect)gameObject; 
        _effects.TryAdd(gameObject.Id, effect);
        effect.Room = this;
        var spawnPacket = new S_SpawnEffect
        {
            Object = gameObject.Info,
            ParentId = parentId, 
            TrailingParent = trailingParent, 
            Duration = duration,
            Scale = scale
        };
        Broadcast(spawnPacket);
    }
    
    // Fade away from game after dying motion on client.
    public void DieAndLeave(int objectId)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

        switch (type)   
        {
            case GameObjectType.Monster:
                if (_monsters.Remove(objectId, out var monster) == false) return;
                Map.ApplyLeave(monster);
                monster.Room = null;
                break;
            
            case GameObjectType.Tower:
                if (_towers.Remove(objectId, out var tower) == false) return;
                Map.ApplyLeave(tower);
                tower.Room = null;
                break;
            
            case GameObjectType.Sheep:
                if (_sheeps.Remove(objectId, out var sheep) == false) return;
                Map.ApplyLeave(sheep);
                sheep.Room = null;
                break;
            
            case GameObjectType.MonsterStatue:
                if (_statues.Remove(objectId, out var statue) == false) return;
                Map.ApplyLeave(statue);
                statue.Room = null;
                break;
            
            case GameObjectType.Portal:
                if (_portal == null) return;
                Map.ApplyLeave(_portal);
                _portal.Room = null;
                _portal = null;
                break;

            case GameObjectType.Fence:
                if (_fences.Remove(objectId, out var fence) == false) return;
                // TODO : Add destroy motion
                Map.ApplyLeave(fence);
                if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt--;
                else GameInfo.SouthFenceCnt--;
                fence.Room = null;
                break;
            
            default: return;
        }
    }

    public void DieTower(int objectId)
    {
        if (_towers.TryGetValue(objectId, out var tower) == false) return;
        Map.ApplyLeave(tower);
    }
    
    // Vanish immediately from game.
    public void LeaveGame(int objectId)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

        switch (type)   
        {
            case GameObjectType.Player:
                if (_players.Remove(objectId, out var player) == false) return;
                player.OnLeaveGame();
                Map.ApplyLeave(player);
                player.Room = null;

                var leavePacket = new S_LeaveGame();
                player.Session?.Send(leavePacket);
                break;
            
            case GameObjectType.Monster:
                if (_monsters.Remove(objectId, out var monster) == false) return;
                Map.ApplyLeave(monster);
                monster.Room = null;
                break;
            
            case GameObjectType.MonsterStatue:
                if (_statues.Remove(objectId, out var statue) == false) return;
                Map.ApplyLeave(statue);
                statue.Room = null;
                break;
            
            case GameObjectType.Tower:
                if (_towers.Remove(objectId, out var tower) == false) return;
                Map.ApplyLeave(tower);
                tower.Room = null;
                break;
            
            case GameObjectType.Sheep:
                if (_sheeps.Remove(objectId, out var sheep) == false) return;
                Map.ApplyLeave(sheep);
                sheep.Room = null;
                break;
            
            case GameObjectType.Fence:
                if (_fences.Remove(objectId, out var fence) == false) return;
                Map.ApplyLeave(fence);
                if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt--;
                else GameInfo.SouthFenceCnt--;
                fence.Room = null;
                break;
            
            case GameObjectType.Projectile:
                if (_projectiles.Remove(objectId, out var projectile) == false) return;
                projectile.Room = null;
                break;
            
            case GameObjectType.Effect:
                if (_effects.Remove(objectId, out var effect) == false) return;
                effect.Room = null;
                break;
            
            case GameObjectType.Portal:
                if (_portal == null) return;
                _portal.Room = null;
                _portal = null;
                break;
        }

        var despawnPacket = new S_Despawn();
        despawnPacket.ObjectIds.Add(objectId);
        foreach (var player in _players.Values.Where(player => player.Id != objectId))
        {
            player.Session?.Send(despawnPacket);
        }
    }

    public void LeaveGameOnlyServer(int objectId)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

        switch (type)   
        {
            case GameObjectType.Projectile:
                if (_projectiles.Remove(objectId, out var projectile) == false) return;
                projectile.Room = null;
                break;
        }
    }

    public Player? FindPlayer(Func<GameObject, bool> condition)
    {
        return _players.Values.FirstOrDefault(condition.Invoke);
    }
    
    public void Broadcast(IMessage packet)
    {
        lock (_lock)
        {
            foreach (var p in _players.Values)
            {
                p.Session?.Send(packet);
            }
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        ClearJobQueue();
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        foreach (var player in _players.Values)
        {
            player.OnLeaveGame();
            player.Room = null;
        }
        
        foreach (var monster in _monsters.Values)
        {
            monster.Room = null;
        }
        
        foreach (var tower in _towers.Values)
        {
            tower.Room = null;
        }
        
        foreach (var sheep in _sheeps.Values)
        {
            sheep.Room = null;
        }
        
        foreach (var fence in _fences.Values)
        {
            fence.Room = null;
        }
        
        foreach (var effect in _effects.Values)
        {
            effect.Room = null;
        }
        
        foreach (var projectile in _projectiles.Values)
        {
            projectile.Room = null;
        }
        
        if (_portal != null)
        {
            _portal.Room = null;
            _portal = null;
        }
        
        if (_storage != null)
        {
            _storage.Room = null;
            _storage = null;
        }
        
        _players.Clear();
        _monsters.Clear();
        _towers.Clear();
        _sheeps.Clear();
        _fences.Clear();
        _effects.Clear();
        _projectiles.Clear();
        _statues.Clear();
        Buffs.Clear();
        GameInfo = new GameInfo(new Dictionary<int, Player>(), 1);
        Map.Room = null;
        GameData = new GameManager.GameData();
        TutorialSpawnFlag = false;
        RoundTime = 24;
        _round = 0;
        _timeSendTime = 0;
        RoomActivated = false;
        Enchant = null;
    }
    
    ~GameRoom()
    {
        Dispose(false);
    }
}
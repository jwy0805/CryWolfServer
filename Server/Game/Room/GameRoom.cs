using System.Diagnostics;
using System.Numerics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Resources;

namespace Server.Game;

public partial class GameRoom : JobSerializer
{
    private bool _tutorialSet;
    private Player _npc = new();
    
    private readonly object _lock = new();
    
    private readonly Dictionary<int, Player> _players = new();
    private readonly Dictionary<int, Tower> _towers = new();
    private readonly Dictionary<int, Monster> _monsters = new();
    private readonly Dictionary<int, MonsterStatue> _statues = new();
    private readonly Dictionary<int, Sheep> _sheeps = new();
    private readonly Dictionary<int, Fence> _fences = new();
    private readonly Dictionary<int, Effect> _effects = new();
    private readonly Dictionary<int, Projectile> _projectiles = new();
    private readonly Dictionary<int, Portal> _portals = new();
    
    private readonly List<TowerSlot> _northTowers = new();
    private readonly List<TowerSlot> _southTowers = new();
    private readonly List<MonsterSlot> _northMonsters = new();
    private readonly List<MonsterSlot> _southMonsters = new();
    
    private int _storageLevel = 0;
    private int _roundTime = 19;
    private int _round = 0;
    private readonly long _interval = 1000;
    private long _timeSendTime;
    
    public readonly Stopwatch Stopwatch = new();
    public HashSet<Buff> Buffs { get; } = new();
    public GameInfo GameInfo { get; set; }
    public List<UnitSize> UnitSizeList { get; set; } = new();
    public int Round => _round;
    public int RoomId { get; set; }
    public bool RoomActivated { get; set; }
    public Map Map { get; private set; } = new();
    public int MapId { get; set; }
    public GameManager.GameData GameData { get; set; } = new();
    
    public void Init(int mapId)
    {
        GameData = GameManager.Instance.GameDataCache[mapId];
        Map.GameData = GameData;
        MapId = mapId;
        Map.LoadMap(mapId);
        Map.MapSetting();
        Map.Room = this; 
        
        UnitSizeMapping();
        GameInit();
    }

    public void Update()
    {
        if (RoomActivated == false)
        {
            return;
        }
        
        Flush();
        SetTimeAndRound();
        UpdateBuffs();
    }

    private void UnitSizeMapping()
    {
        int[] unitIds = Enum.GetValues(typeof(UnitId)) as int[] ?? Array.Empty<int>();
        
        foreach (var unitId in unitIds)
        {
            DataManager.UnitDict.TryGetValue(unitId, out var unitData);
            StatInfo stat = new StatInfo();
            stat.MergeFrom(unitData?.stat);
            UnitSizeList.Add(new UnitSize((UnitId)unitId, stat.SizeX, stat.SizeZ));
        }
    }
    
    private void SetTimeAndRound()
    {
        long time = Stopwatch.ElapsedMilliseconds;
        if (time < _timeSendTime + _interval || time < 1000) return;

        Broadcast(new S_Time { Time = _roundTime, Round = _round});
        _roundTime--;
        
        // Tutorial
        if (_roundTime < 15 && _tutorialSet == false)
        {
            // SetTutorialRound(_round);
            _tutorialSet = true;
        }
        // Tutorial
        
        if (_roundTime < 0) 
        {
            InitRound();
            // T_SpawnMonstersInNewRound();
            // SpawnMonstersInNewRound();
        }
        
        if (_roundTime < 10 && _round != 0) CheckMonsters();
        _timeSendTime = time;
    }
    
    public void EnterGame(GameObject gameObject)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);
        
        switch (type)
        {
            case GameObjectType.Player:
                var player = (Player)gameObject;
                _players.Add(gameObject.Id, player);
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
                    foreach (var t in _towers.Values) spawnPacket.Objects.Add(t.Info);
                    foreach (var e in _effects.Values) spawnPacket.Objects.Add(e.Info);
                    foreach (var r in _portals.Values) spawnPacket.Objects.Add(r.Info);
                    player.Session?.Send(spawnPacket);
                }
                break;
            
            case GameObjectType.Tower:
                Tower tower = (Tower)gameObject;
                gameObject.Info.Name = Enum.Parse(typeof(UnitId), tower.UnitId.ToString()).ToString();
                gameObject.Info.PosInfo = gameObject.PosInfo;
                tower.Info = gameObject.Info;
                _towers.Add(gameObject.Id, tower);
                Map.ApplyMap(tower);
                tower.Update();
                break;
            
            case GameObjectType.Monster:
                Monster monster = (Monster)gameObject;
                gameObject.Info.Name = Enum.Parse(typeof(UnitId), monster.UnitId.ToString()).ToString();
                gameObject.PosInfo.Dir = 180;
                gameObject.Info.PosInfo = gameObject.PosInfo;
                monster.Info = gameObject.Info;
                _monsters.Add(gameObject.Id, monster);
                Map.ApplyMap(monster);
                monster.Update();
                break;
            
            case GameObjectType.MonsterStatue:
                MonsterStatue statue = (MonsterStatue)gameObject;
                string? monsterName = Enum.Parse(typeof(UnitId), statue.UnitId.ToString()).ToString();
                gameObject.Info.Name = string.Concat(monsterName, "Statue");
                statue.Info = gameObject.Info;
                _statues.Add(gameObject.Id, statue);
                Map.ApplyMap(statue);
                statue.Update();
                break;
            
            case GameObjectType.Fence:
                Fence fence = (Fence)gameObject;
                fence.Info = gameObject.Info;
                _fences.Add(gameObject.Id, fence);
                fence.Room = this;
                Map.ApplyMap(fence);
                break;
            
            case GameObjectType.Sheep:
                Sheep sheep = (Sheep)gameObject;
                gameObject.PosInfo.State = State.Idle;
                gameObject.Info.Name = "Sheep";
                sheep.Info = gameObject.Info;
                _sheeps.Add(gameObject.Id, sheep);
                sheep.Room = this;
                Map.ApplyMap(sheep);
                sheep.Update();
                break;
            
            case GameObjectType.Resource:
                Resource resource = (Resource)gameObject;
                resource.Info.Name = Enum.Parse(typeof(ResourceId), resource.ResourceNum.ToString()).ToString();
                resource.Room = this;
                resource.Player = gameObject.Player;
                resource.Info = gameObject.Info;
                resource.Init();
                resource.Update();
                break;
            
            case GameObjectType.Portal:
                Portal portal = (Portal)gameObject;
                _portals.Add(gameObject.Id, portal);
                portal.Room = this;
                Map.ApplyMap(portal);
                break;
        }
        // 타인에게 정보 전송
        {
            var spawnPacket = new S_Spawn();
            spawnPacket.Objects.Add(gameObject.Info);
            foreach (var player in _players.Values.Where(player => player.Id != gameObject.Id))
            {
                player.Session?.Send(spawnPacket);
            }
        }
    }

    private void EnterGameProjectile(GameObject gameObject, Vector3 targetPos, float speed, int parentId)
    {
        var projectile = (Projectile)gameObject;
        _projectiles.Add(projectile.Id, projectile);
        projectile.Room = this;
        var destVector = new DestVector { X = targetPos.X, Y = targetPos.Y + 0.5f, Z = targetPos.Z };
        var spawnPacket = new S_SpawnProjectile
        {
            Object = gameObject.Info, ParentId = parentId, DestPos = destVector, MoveSpeed = speed
        };
        Broadcast(spawnPacket);
    }

    private void EnterGameEffect(GameObject gameObject, int parentId, bool trailingParent, int duration = 2000)
    {
        var effect = (Effect)gameObject;
        _effects.Add(gameObject.Id, effect);
        effect.Room = this;
        var spawnPacket = new S_SpawnEffect
        {
            Object = gameObject.Info, ParentId = parentId, TrailingParent = trailingParent, Duration = duration
        };
        Broadcast(spawnPacket);
    }

    public void EnterGameParent(GameObject gameObject, GameObject parent)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

        switch (type)
        {
            case GameObjectType.Effect:
                Effect effect = (Effect)gameObject;
                _effects.Add(gameObject.Id, effect);
                effect.Parent = parent;
                effect.Room = this;
                effect.Update();
                break;
        }

        // var spawnPacket = new S_SpawnParent { Object = gameObject.Info, ParentId = parent.Id };
        // Broadcast(spawnPacket);
    }

    public void EnterGameTarget(GameObject gameObject, GameObject parent, GameObject target)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

        switch (type)
        {
            case GameObjectType.Effect:
                Effect effect = (Effect)gameObject;
                _effects.Add(gameObject.Id, effect);
                effect.Parent = parent;
                effect.Target = target;
                effect.Room = this;
                effect.Update();
                break;
        }

        // S_SpawnParent spawnPacket = new S_SpawnParent { Object = gameObject.Info, ParentId = target.Id };
        // foreach (var player in _players.Values.Where(player => player.Id != gameObject.Id))
        // {
        //     player.Session.Send(spawnPacket);
        // }
    }
    
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
            default: return;
        }
    }

    public void DieTower(int objectId)
    {
        if (_towers.TryGetValue(objectId, out var tower) == false) return;
        Map.ApplyLeave(tower);
    }
    
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

                S_LeaveGame leavePacket = new S_LeaveGame();
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
            
            case GameObjectType.Fence:
                if (_fences.Remove(objectId, out var fence) == false) return;
                Map.ApplyLeave(fence);
                
                if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt--;
                else  GameInfo.SouthFenceCnt--;
                
                fence.Room = null;
                break;
            
            case GameObjectType.Sheep:
                if (_sheeps.Remove(objectId, out var sheep) == false) return;
                Map.ApplyLeave(sheep);
                sheep.Room = null;
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
                if (_portals.Remove(objectId, out var portal) == false) return;
                portal.Room = null;
                break;
        }

        S_Despawn despawnPacket = new S_Despawn();
        despawnPacket.ObjectIds.Add(objectId);
        foreach (var player in _players.Values)
        {
            if (player.Id != objectId) player.Session?.Send(despawnPacket);
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
        foreach (var player in _players.Values)
        {
            if (condition.Invoke(player)) return player;
        }

        return null;
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
}
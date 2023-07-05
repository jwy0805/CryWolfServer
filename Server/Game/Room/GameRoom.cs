using System.Numerics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class GameRoom
{
    private readonly object _lock = new();
    public int RoomId { get; set; }

    private Dictionary<int, Player> _players = new();
    private Dictionary<int, Tower> _towers = new();
    private Dictionary<int, Monster> _monsters = new();
    private Dictionary<int, Sheep> _sheeps = new();
    private Dictionary<int, Fence> _fences = new();

    public Map Map { get; private set; } = new Map();

    private int _storageLevel = 0;

    public int StorageLevel
    {
        get => _storageLevel;
        set
        {
            _storageLevel = value;
            if (_storageLevel > 3) _storageLevel = 3;
            GameData.StorageLevel = _storageLevel;

            lock (_lock)
            {
                // 울타리 생성
                if (_storageLevel != 1 && _fences.Count > 0)
                {
                    foreach (var fenceId in _fences.Keys) LeaveGame(fenceId);
                }
                
            }
        }
    }
    
    private void GameInit()
    {
        
    }
    
    public void Init(int mapId)
    {
        Map.LoadMap(mapId);
        Map.MapSetting();
        GameInit();
    }

    public void EnterGame(GameObject gameObject)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);
        
        lock (_lock)
        {
            switch (type)
            {
                case GameObjectType.Player:
                    Player player = (Player)gameObject;
                    _players.Add(gameObject.Id, player);
                    player.Room = this;

                    // 본인에게 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame { Player = player.Info };
                    player.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (var p in _players.Values)
                    {
                        if (player != p) spawnPacket.Objects.Add(p.Info);
                    }

                    foreach (var t in _towers.Values)
                    {
                        spawnPacket.Objects.Add(t.Info);
                    }
                    
                    player.Session.Send(spawnPacket);
                }
                    break;
                case GameObjectType.Tower:
                    Tower tower = (Tower)gameObject;
                    gameObject.Info.Name = Enum.Parse(typeof(TowerId), tower.TowerNo.ToString()).ToString();
                    gameObject.PosInfo.State = State.Idle;
                    gameObject.Info.PosInfo = gameObject.PosInfo;
                    tower.Info = gameObject.Info;
                    _towers.Add(gameObject.Id, tower);
                    tower.Room = this;
                    break;
                case GameObjectType.Monster:
                    Monster monster = (Monster)gameObject;
                    gameObject.Info.Name = Enum.Parse(typeof(MonsterId), monster.MonsterNo.ToString()).ToString();
                    gameObject.PosInfo.State = State.Idle;
                    gameObject.Info.PosInfo = gameObject.PosInfo;
                    monster.Info = gameObject.Info;
                    _monsters.Add(gameObject.Id, monster);
                    monster.Room = this;
                    break;
                case GameObjectType.Fence:
                    Fence fence = (Fence)gameObject;
                    fence.Info = gameObject.Info;
                    _fences.Add(gameObject.Id, fence);
                    fence.Room = this;
                    break;
            }
            // 타인에게 정보 전송
            {
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Objects.Add(gameObject.Info);
                foreach (Player p in _players.Values)
                {
                    if (p.Id != gameObject.Id)
                        p.Session.Send(spawnPacket);
                }
            }
        }
    }

    public void LeaveGame(int objectId)
    {
        lock (_lock)
        {
            if (_players.Remove(objectId, out var player) == false) return;
            
            player.Room = null;
            
            // 본인에게 정보 전송
            {
                S_LeaveGame leavePacket = new();
                player.Session.Send(leavePacket);
            }
            
            // 타인에게 정보 전송
            {
                S_Despawn despawnPacket = new();
                despawnPacket.ObjectIds.Add(player.Info.ObjectId);
                foreach (var p in _players.Values)
                {
                    if (player != p) p.Session.Send(despawnPacket);
                }
            }
        }
    }
    
    public void HandlePlayerMove(Player player, C_PlayerMove pMovePacket)
    {
        if (player == null) return;

        lock (_lock)
        {
            S_PlayerMove playerMovePacket = new S_PlayerMove
            {
                State = pMovePacket.State,
                ObjectId = player.Id,
                DestPos = pMovePacket.DestPos
            };
            
            Broadcast(playerMovePacket);
        }
    }
    
    public void HandleMove(Player player, C_Move movePacket)
    {
        if (player == null) return;

        lock (_lock)
        {
            PositionInfo movePosInfo = movePacket.PosInfo;
            ObjectInfo info = player.Info;

            info.PosInfo.State = movePosInfo.State;
            info.PosInfo.Dir = movePosInfo.Dir;

            S_Move resMovePacket = new S_Move
            {
                ObjectId = player.Info.ObjectId,
                PosInfo = movePacket.PosInfo
            };
            
            Broadcast(resMovePacket);
        }
    }

    public void HandleSpawn(Player player, C_Spawn spawnPacket)
    {
        if (player == null) return;
        GameObjectType type = spawnPacket.Type;

        lock (_lock)
        {
            switch (type)
            {
                case GameObjectType.Tower:
                    Tower tower = ObjectManager.Instance.Add<Tower>();
                    tower.PosInfo = spawnPacket.PosInfo;
                    tower.TowerNo = spawnPacket.Num;
                    EnterGame(tower);
                    break;
                case GameObjectType.Monster:
                    Monster monster = ObjectManager.Instance.Add<Monster>();
                    monster.PosInfo = spawnPacket.PosInfo;
                    monster.MonsterNo = spawnPacket.Num;
                    EnterGame(monster);
                    break;
            }
        }
    }

    public GameObject? FindTarget(List<GameObjectType> typeList, GameObject gameObject)
    {
        // 어그로 끌린 상태면 리턴하는 코드
        //
        Dictionary<int, GameObject> targetDict = new();

        foreach (var t in typeList)
        {
            switch (t)
            {
                case GameObjectType.Monster:
                    foreach (var (key, monster) in _monsters) targetDict.Add(key, monster);
                    break;
                case GameObjectType.Tower:
                    foreach (var (key, tower) in _towers) targetDict.Add(key, tower);
                    break;
                case GameObjectType.Sheep:
                    foreach (var (key, sheep) in _sheeps) targetDict.Add(key, sheep);
                    break;
                case GameObjectType.Fence:
                    foreach (var (key, fence) in _fences) targetDict.Add(key, fence);
                    break;
            }
        }

        if (targetDict.Count == 0) return null;
        GameObject? target = null;
        
        float closestDist = 5000f;
        foreach (var (key, obj) in targetDict)
        {
            PositionInfo pos = obj.PosInfo;
            Vector3 targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
            bool targetable = true; // Stat의 Targetable 반드시 설정
            float dist = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
            if (dist < closestDist && targetable)
            {
                closestDist = dist;
                target = obj;
            }
        }

        return target;
    }

    private void SpawnFence(int lv = 1)
    {
        Vector3[] fencePos = GameData.GetPos(GameData.FenceCnt[lv], GameData.FenceRow[lv], GameData.FenceStartPos[lv]);
        float[] fenceRotation = GameData.GetRotation(GameData.FenceCnt[lv], GameData.FenceRow[lv]);

        for (int i = 0; i < GameData.FenceCnt[lv]; i++)
        {
            
        }
    }
    
    public void Broadcast(IMessage packet)
    {
        lock (_lock)
        {
            foreach (var p in _players.Values)
            {
                p.Session.Send(packet);
            }
        }
    }
}
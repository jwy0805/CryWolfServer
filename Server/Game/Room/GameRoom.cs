using System.Numerics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public partial class GameRoom : JobSerializer
{
    private readonly object _lock = new();
    public int RoomId { get; set; }

    private Dictionary<int, Player> _players = new();
    private Dictionary<int, Tower> _towers = new();
    private Dictionary<int, Monster> _monsters = new();
    private Dictionary<int, Sheep> _sheeps = new();
    private Dictionary<int, Fence> _fences = new();

    public Map Map { get; private set; } = new();

    private int _storageLevel = 0;

    public void Init(int mapId)
    {
        Map.LoadMap(mapId);
        Map.MapSetting();
        GameInit();
    }

    public void Update()
    {
        Flush();
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
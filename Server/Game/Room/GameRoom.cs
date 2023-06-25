using Google.Protobuf;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class GameRoom
{
    private readonly object _lock = new();
    public int RoomId { get; set; }

    private Dictionary<int, Player> _players = new();
    private Dictionary<int, Tower> _towers = new();

    public void EnterGame(GameObject gameObject)
    {
        if (gameObject == null) return;

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
                    _towers.Add(gameObject.Id, tower);
                    tower.Room = this;
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

        lock (_lock)
        {
            ObjectInfo info = player.Info;

            switch (spawnPacket.Type)
            {
                case GameObjectType.Tower:
                    Tower tower = ObjectManager.Instance.Add<Tower>();
                    tower.PosInfo = spawnPacket.PosInfo;
                    EnterGame(tower);
                    break;
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
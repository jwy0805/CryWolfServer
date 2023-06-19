using Google.Protobuf;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class GameRoom
{
    private readonly object _lock = new();
    public int RoomId { get; set; }

    private List<Player> _players = new();

    public void EnterGame(Player newPlayer)
    {
        if (newPlayer == null) return;

        lock (_lock)
        {
            _players.Add(newPlayer);
            newPlayer.Room = this;
            // 본인에게 정보 전송
            {
                S_EnterGame enterPacket = new() { Player = newPlayer.Info };
                newPlayer.Session.Send(enterPacket);

                S_Spawn spawnPacket = new();
                foreach (var p in _players)
                {
                    if (newPlayer != p) spawnPacket.Objects.Add(p.Info);
                }
                newPlayer.Session.Send(spawnPacket);
            }
            
            // 타인에게 정보 전송
            {
                S_Spawn spawnPacket = new();
                spawnPacket.Objects.Add(newPlayer.Info);
                foreach (var p in _players)
                {
                    if (newPlayer != p) p.Session.Send(spawnPacket);
                }
            }
        }
    }

    public void LeaveGame(int playerId)
    {
        lock (_lock)
        {
            Player player = _players.Find(p => p.Info.ObjectId == playerId);
            if (player == null) return;

            _players.Remove(player);
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
                foreach (var p in _players)
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
            foreach (var p in _players)
            {
                p.Session.Send(packet);
            }
        }
    }
}
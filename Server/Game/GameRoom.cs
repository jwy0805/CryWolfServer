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
                foreach (var player in _players)
                {
                    if (newPlayer != player) spawnPacket.Objects.Add(player.Info);
                }
                newPlayer.Session.Send(spawnPacket);
            }
            
            // 타인에게 정보 전송
            {
                S_Spawn spawnPacket = new();
                spawnPacket.Objects.Add(newPlayer.Info);
                foreach (var player in _players)
                {
                    if (newPlayer != player) player.Session.Send(spawnPacket);
                }
            }
        }
    }

    public void LeaveGame(int playerId)
    {
        lock (_lock)
        {
            
        }
    }
}
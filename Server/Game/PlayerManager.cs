namespace Server.Game;

public class PlayerManager
{
    public static PlayerManager Instance { get; } = new();

    private readonly object _lock = new();
    private Dictionary<int, Player> _players = new();
    private int _playerId = 1;

    public Player Add()
    {
        Player player = new();

        lock (_lock)
        {
            player.Info.ObjectId = _playerId;
            _players.Add(_playerId, player);
            _playerId++;
        }

        return player;
    }

    public bool Remove(int playerId)
    {
        lock (_lock)
        {
            return _players.Remove(playerId);
        }
    }

    public Player? Find(int playerId)
    {
        lock (_lock)
        {
            return _players.TryGetValue(playerId, out var player) ? player : null;
        }
    }
}
using Server.Game;

namespace Server.DB;

public class DbTransaction : JobSerializer
{
    public static DbTransaction Instance { get; } = new();
}
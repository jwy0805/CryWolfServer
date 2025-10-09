using Google.Protobuf.Protocol;

namespace Server.Game.AI;

public class WorldSnapshot
{
    public Player SheepPlayer { get; init; }
    public Player WolfPlayer { get; init; }
    public UnitId[] SheepUnits { get; init; } = Array.Empty<UnitId>();
    public UnitId[] WolfUnits { get; init; } = Array.Empty<UnitId>();
    
    public int RoundTimeLeft { get; init; }
    
    public int SheepResource { get; init; }
    public int WolfResource { get; init; }
    
    public int SheepBaseLevel { get; init; }
    public int WolfBaseLevel { get; init; }
    
    public int SheepMaxPop { get; init; }
    public int SheepPop { get; init; }
    public int WolfMaxPop { get; init; }
    public int WolfPop { get; init; }
    
    public IReadOnlyDictionary<UnitId, int> SheepUnitCounts { get; init; } = new Dictionary<UnitId, int>();
    public IReadOnlyDictionary<UnitId, int> WolfUnitCounts { get; init; } = new Dictionary<UnitId, int>();
}
using Google.Protobuf.Protocol;

namespace Server.Game;

public readonly record struct UpkeepExcess(UnitId UnitId, int PeakCount, int LimitAtPeak, int PeakExcess);

public class UpkeepTracker<T>
{
    private readonly Func<T, UnitId> _selector;
    private readonly GameRoom _room;
    private readonly Faction _faction;
    private readonly Dictionary<UnitId, int> _peakCount = new();
    private readonly Dictionary<UnitId, int> _peakExcess = new(); // 초과된 count
    private readonly Dictionary<UnitId, int> _limitAtPeak = new();
    
    private int PopDivThreshold => _faction == Faction.Sheep
        ? _room.GameInfo.NorthMaxTower / 3
        : _room.GameInfo.NorthMaxMonster / 3;
    
    public UpkeepTracker(Func<T, UnitId> selector, GameRoom room, Faction faction)
    {
        _selector = selector;
        _room = room;
        _faction = faction;
    }
    
    public bool HasAnyExcessThisRound => _peakExcess.Values.Any(v => v > 0);
    public bool AlreadyMaxed(UnitId unitId, int population) 
        => _peakExcess.TryGetValue(unitId, out var count) && count > PopDivThreshold;
    
    // private static int CeilDiv(int n, int d) => (n + d - 1) / d;

    public void Observe(IEnumerable<T> source, int population)
    {
        if (population <= 0) return;
        int limit = PopDivThreshold;
        // Counts per UnitId
        var counts = source.Select(_selector)
            .Where(id => !EqualityComparer<UnitId>.Default.Equals(id, default))
            .GroupBy(id => id)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.Count());

        foreach (var (unitId, count) in counts)
        {
            var prev = _peakCount.GetValueOrDefault(unitId, 0);
            if (count > prev) _peakCount[unitId] = count;
            
            // Calculate excess at this limit
            int excess = Math.Max(0, count - limit);
            var prevExcess = _peakExcess.GetValueOrDefault(unitId, 0);
            if (excess > prevExcess)
            {
                _peakExcess[unitId] = excess;
                _limitAtPeak[unitId] = limit;
            }
        }
    }
    
    // 다음 라운드 정산용으로 꺼내고 리셋
    public List<UpkeepExcess> FinalizeAndReset()
    {
        var list = _peakExcess
            .Where(kv => kv.Value > 0)
            .Select(kv =>
            {
                var unitId = kv.Key;
                var peakEx = kv.Value;
                var limitAt = _limitAtPeak.GetValueOrDefault(unitId, 0);
                var peakCnt = _peakCount.GetValueOrDefault(unitId, 0);
                return new UpkeepExcess(unitId, peakCnt, limitAt, peakEx);
            })
            .OrderByDescending(x => x.PeakExcess)
            .ToList();
        
        _peakCount.Clear();
        _peakExcess.Clear();
        _limitAtPeak.Clear();
        
        return list;
    }
}
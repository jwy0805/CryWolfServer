using Google.Protobuf.Protocol;

namespace Server.Data;

[Serializable]
public class MonsterData
{
    public int id;
    public int no;
    public string name;
    public StatInfo stat;
}

public class TowerData
{
    public int id;
    public int no;
    public string name;
    public StatInfo stat;
}

public class FenceData
{
    public int id;
    public int no;
    public string name;
    public StatInfo stat;
}

public class PlayerData
{
    public int no;
    public StatInfo stat;
}

[Serializable]
public class MonsterLoader : ILoader<int, MonsterData>
{
    public List<MonsterData> monsters = new();

    public Dictionary<int, MonsterData> MakeDict()
    {
        return monsters.ToDictionary(monster => monster.no);
    }
}

[Serializable]
public class TowerLoader : ILoader<int, TowerData>
{
    public List<TowerData> towers = new();

    public Dictionary<int, TowerData> MakeDict()
    {
        return towers.ToDictionary(tower => tower.no);
    }
}

[Serializable]
public class FenceLoader : ILoader<int, FenceData>
{
    public List<FenceData> fences = new();

    public Dictionary<int, FenceData> MakeDict()
    {
        return fences.ToDictionary(fence => fence.no);
    }
}

[Serializable]
public class PlayerLoader : ILoader<int, PlayerData>
{
    public List<PlayerData> players = new();
    
    public Dictionary<int, PlayerData> MakeDict()
    {
        return players.ToDictionary(player => player.no);
    }
}
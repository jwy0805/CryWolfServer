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
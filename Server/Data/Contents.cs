using Google.Protobuf.Protocol;

namespace Server.Data;

[Serializable]
public class MonsterData
{
    public int id;
    public int no;
    public string name;
    public List<string> unitRole;
    public StatInfo stat;
}

[Serializable]
public class TowerData
{
    public int id;
    public int no;
    public string name;
    public List<string> unitRole;
    public StatInfo stat;
}

[Serializable]
public class FenceData
{
    public int id;
    public int no;
    public string name;
    public StatInfo stat;
}

[Serializable]
public class ObjectData
{
    public int no;
    public StatInfo stat;
}

[Serializable]
public class SkillData
{
    public int id;
    public string explanation;
    public int cost;
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
public class ObjectLoader : ILoader<int, ObjectData>
{
    public List<ObjectData> objects = new();
    
    public Dictionary<int, ObjectData> MakeDict()
    {
        return objects.ToDictionary(player => player.no);
    }
}

[Serializable]
public class SkillLoader : ILoader<int, SkillData>
{
    public List<SkillData> skills = new();

    public Dictionary<int, SkillData> MakeDict()
    {
        return skills.ToDictionary(skill => skill.id);
    }
}
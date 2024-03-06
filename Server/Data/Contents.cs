using Google.Protobuf.Protocol;

namespace Server.Data;

[Serializable]
public class UnitData
{
    public int id;
    public string name;
    public string camp;
    public List<string> unitRole;
    public StatInfo stat;
}

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
public class UnitLoader : ILoader<int, UnitData>
{
    public List<UnitData> units = new();
    
    public Dictionary<int, UnitData> MakeDict()
    {
        return units.ToDictionary(unit => unit.id);
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
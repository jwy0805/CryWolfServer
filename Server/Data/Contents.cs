using Google.Protobuf.Protocol;
using Newtonsoft.Json;

// ReSharper disable UnassignedField.Global

namespace Server.Data;

[Serializable]
public class UnitData
{
    public int Id;
    public string Name;
    public string Faction;
    public Role UnitRole;
    public UnitClass UnitClass;
    public Species UnitSpecies;
    public UnitRegion Region;
    public string RecommendedLocation;
    public StatInfo Stat;

    [JsonConstructor]
    public UnitData(int id, string name, string faction, Role unitRole, UnitClass unitClass, Species unitSpecies,
        UnitRegion unitRegion, string recommendedLocation, StatInfo stat)
    {
        Id = id;
        Name = name;
        Faction = faction;
        UnitRole = unitRole;
        UnitClass = unitClass;
        UnitSpecies = unitSpecies;
        Region = unitRegion;
        RecommendedLocation = recommendedLocation;
        Stat = stat;
    }
}

[Serializable]
public class FenceData
{
    public int Id;
    public int No;
    public string Name;
    public StatInfo Stat;

    [JsonConstructor]
    public FenceData(int id, int no, string name, StatInfo stat)
    {
        Id = id;
        No = no;
        Name = name;
        Stat = stat;
    }
}

[Serializable]
public class ObjectData
{
    public int Id;
    public string Name;
    public StatInfo Stat;

    [JsonConstructor]
    public ObjectData(StatInfo stat, string name)
    {
        Stat = stat;
        Name = name;
    }
}

[Serializable]
public class SkillData
{
    public int Id;
    public string Explanation;
    public int Cost;
    public float Value;
    public float Coefficient;
    public SkillType Type;

    [JsonConstructor]
    public SkillData(string explanation)
    {
        Explanation = explanation;
    }
}

[Serializable]
public class UnitLoader : ILoader<int, UnitData>
{
    public List<UnitData> Units = new();
    
    public Dictionary<int, UnitData> MakeDict()
    {
        return Units.ToDictionary(unit => unit.Id);
    }
}

[Serializable]
public class FenceLoader : ILoader<int, FenceData>
{
    public List<FenceData> Fences = new();

    public Dictionary<int, FenceData> MakeDict()
    {
        return Fences.ToDictionary(fence => fence.No);
    }
}

[Serializable]
public class ObjectLoader : ILoader<int, ObjectData>
{
    public List<ObjectData> Objects = new();
    
    public Dictionary<int, ObjectData> MakeDict()
    {
        return Objects.ToDictionary(player => player.Id);
    }
}

[Serializable]
public class SkillLoader : ILoader<int, SkillData>
{
    public List<SkillData> Skills = new();

    public Dictionary<int, SkillData> MakeDict()
    {
        return Skills.ToDictionary(skill => skill.Id);
    }
}
namespace Server.Data;

public interface ILoader<TKey, TValue>
{
    Dictionary<TKey, TValue> MakeDict();
}

public class DataManager
{
    public static Dictionary<int, UnitData> UnitDict { get; private set; } = new();
    public static Dictionary<int, FenceData> FenceDict { get; private set; } = new();
    public static Dictionary<int, ObjectData> ObjectDict { get; private set; } = new();
    public static Dictionary<int, SkillData> SkillDict { get; private set; } = new();

    public static void LoadData()
    {
        UnitDict = LoadJson<UnitLoader, int, UnitData>("UnitData")!.MakeDict();
        FenceDict = LoadJson<FenceLoader, int, FenceData>("FenceData")!.MakeDict();
        ObjectDict = LoadJson<ObjectLoader, int, ObjectData>("ObjectData")!.MakeDict();
        SkillDict = LoadJson<SkillLoader, int, SkillData>("SkillData")!.MakeDict();
    }

    private static TLoader? LoadJson<TLoader, TKey, TValue>(string data) where TLoader : ILoader<TKey, TValue>
    {
        var path = Environment.GetEnvironmentVariable("DATA_PATH") ??
                   "/Users/jwy/Documents/Dev/CryWolf/Common";
        var text = File.ReadAllText($"{path}/{data}.json");
        return Newtonsoft.Json.JsonConvert.DeserializeObject<TLoader>(text);
    }
}
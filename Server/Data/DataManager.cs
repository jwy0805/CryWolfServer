namespace Server.Data;

public interface ILoader<TKey, TValue>
{
    Dictionary<TKey, TValue> MakeDict();
}
public class DataManager
{
    public static Dictionary<int, MonsterData> MonsterDict { get; private set; } = new();
    public static Dictionary<int, TowerData> TowerDict { get; private set; } = new();
    public static Dictionary<int, FenceData> FenceDict { get; private set; } = new();

    public static void LoadData()
    {
        MonsterDict = LoadJson<MonsterLoader, int, MonsterData>("MonsterData")!.MakeDict();
        TowerDict = LoadJson<TowerLoader, int, TowerData>("TowerData")!.MakeDict();
        FenceDict = LoadJson<FenceLoader, int, FenceData>("FenceData")!.MakeDict();
    }

    private static TLoader? LoadJson<TLoader, TKey, TValue>(string path) where TLoader : ILoader<TKey, TValue>
    {
        string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/{path}.json");
        return Newtonsoft.Json.JsonConvert.DeserializeObject<TLoader>(text);
    }
}
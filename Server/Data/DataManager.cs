namespace Server.Data;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}
public class DataManager
{
    public static void LoadData()
    {
        
    }
}
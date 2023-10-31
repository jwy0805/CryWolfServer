namespace Server.Data;

[Serializable]
public class ServerConfig
{
    public string dataPath;
    public string connectionString;
}

public class ConfigManager
{
    public static ServerConfig? Config { get; private set; }

    public static void LoadConfig()
    {
        // string text = File.ReadAllText("config.json");
        string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string text = File.ReadAllText(Path.Combine(homePath,
            "Documents/dev/CryWolf/Server/Server/bin/Debug/net6.0/config.json"));
        Config = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerConfig>(text)!;
    }
}
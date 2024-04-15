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
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var text = File.ReadAllText(Path.Combine(homePath,
            "Documents/dev/CryWolf/Server/Server/bin/Debug/net6.0/config.json"));
        Config = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerConfig>(text);
    }
}
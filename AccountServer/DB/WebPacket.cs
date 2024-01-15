namespace AccountServer.DB;

public class CreateAccountPacketRequired
{
    public string AccountName { get; set; }
    public string Password { get; set; }
}

public class CreateAccountPacketResponse
{
    public bool CreateOK { get; set; }
}

public class LoginAccountPacketRequired
{
    public string AccountName { get; set; }
    public string Password { get; set; }
}

public class ServerInfo
{
    public string Name { get; set; }
    public string IP { get; set; }
    public int CrowdedLevel { get; set; }
}

public class LoginAccountPacketResponse
{
    public bool LoginOK { get; set; }
    public List<ServerInfo> ServerList { get; set; } = new();
}
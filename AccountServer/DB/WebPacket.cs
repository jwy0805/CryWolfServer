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
    public int Port { get; set; }
    public int BusyScore { get; set; }
}

public class LoginAccountPacketResponse
{
    public bool LoginOK { get; set; }
    public int AccountId { get; set; }
    public int Token { get; set; }
    public List<ServerInfo> ServerList { get; set; } = new();
}
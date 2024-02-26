namespace AccountServer.DB;

public class CreateUserAccountPacketRequired
{
    public string UserName { get; set; }
    public string Password { get; set; }
}

public class CreateUserAccountPacketResponse
{
    public bool CreateOK { get; set; }
}

public class LoginUserAccountPacketRequired
{
    public string UserName { get; set; }
    public string Password { get; set; }
}

public class ServerInfo
{
    public string Name { get; set; }
    public string IP { get; set; }
    public int Port { get; set; }
    public int BusyScore { get; set; }
}

public class LoginUserAccountPacketResponse
{
    public bool LoginOK { get; set; }
    public int UserId { get; set; }
    public int Token { get; set; }
    public List<ServerInfo> ServerList { get; set; } = new();
}
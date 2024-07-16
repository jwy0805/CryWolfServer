namespace Google.Protobuf.Protocol;

#region For Match Making Server

public class MatchSuccessPacketRequired
{
    public int SheepUserId { get; set; }
    public int WolfUserId { get; set; }
    public int MapId { get; set; }
}

public class MatchSuccessPacketResponse
{
    public bool IsSuccess { get; set; }
}

#endregion

#region For API Server

public class GetUserIdPacketRequired
{
    public string UserAccount { get; set; }
}

public class GetUserIdPacketResponse
{
    public int UserId { get; set; }
}

#endregion
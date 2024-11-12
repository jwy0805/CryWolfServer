namespace Google.Protobuf.Protocol;

#region For Match Making Server

public class MatchSuccessPacketRequired
{
    public int SheepUserId { get; set; }
    public int SheepSessionId { get; set; }
    public string SheepUserName { get; set; }
    public int WolfUserId { get; set; }
    public int WolfSessionId { get; set; }
    public string WolfUserName { get; set; }
    public int MapId { get; set; }
    public int SheepRankPoint { get; set; }
    public int WolfRankPoint { get; set; }
    public int WinPointSheep { get; set; }
    public int WinPointWolf { get; set; }
    public int LosePointSheep { get; set; }
    public int LosePointWolf { get; set; }
    public int SheepCharacterId { get; set; }
    public int WolfCharacterId { get; set; }
    public SheepId SheepId { get; set; }
    public EnchantId EnchantId { get; set; }
    public UnitId[] SheepUnitIds { get; set; }
    public UnitId[] WolfUnitIds { get; set; }
    public List<int> SheepAchievements { get; set; }
    public List<int> WolfAchievements { get; set; }
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
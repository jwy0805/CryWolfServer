namespace Google.Protobuf.Protocol;

#region For Match Making Server

public class RewardInfo
{
    public int ItemId { get; set; }
    public ProductType ProductType { get; set; }
    public int Count { get; set; }
}

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
    public CharacterId SheepCharacterId { get; set; }
    public CharacterId WolfCharacterId { get; set; }
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

public class SendMatchInfoPacketRequired
{
    public int SheepUserId { get; set; }
    public int SheepSessionId { get; set; }
    public int WolfUserId { get; set; }
    public int WolfSessionId { get; set; }
}

public class SendMatchInfoPacketResponse
{
    public bool SendMatchInfoOk { get; set; }
}

public class GameResultPacketRequired
{
    public int UserId { get; set; }
    public bool IsWin { get; set; }
}

public class GameResultPacketResponse
{
    public bool GetGameResultOk { get; set; }
}

public class GameRewardPacketRequired
{
    public int WinUserId { get; set; }
    public int WinRankPoint { get; set; }
    public int LoseUserId { get; set; }
    public int LoseRankPoint { get; set; }
}

public class GameRewardPacketResponse
{
    public bool GetGameRewardOk { get; set; }
    public List<RewardInfo> WinnerRewards { get; set; }
    public List<RewardInfo> LoserRewards { get; set; }
}

public class SessionDisconnectPacketRequired
{
    public int UserId { get; set; }
    public int SessionId { get; set; }
}

public class SessionDisconnectPacketResponse
{
    public bool SessionDisconnectOk { get; set; }
}

#endregion
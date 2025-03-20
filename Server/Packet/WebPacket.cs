namespace Google.Protobuf.Protocol;

public class TestApiToSocketRequired
{
    public bool Test { get; set; }
}

public class TestApiToSocketResponse
{
    public bool TestOk { get; set; }
}


#region For Match Making Server

public class RewardInfo
{
    public int ItemId { get; set; }
    public ProductType ProductType { get; set; }
    public int Count { get; set; }
}

public class SingleRewardInfo
{
    public int ItemId { get; set; }
    public ProductType ProductType { get; set; }
    public int Count { get; set; }
    public int Star { get; set; }
}

public class MatchSuccessPacketRequired
{
    public bool IsTestGame { get; set; }
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

public class SinglePlayStartPacketRequired
{
    public int UserId { get; set; }
    public Faction UserFaction { get; set; }
    public UnitId[] UnitIds { get; set; }
    public int CharacterId { get; set; }
    public int AssetId { get; set; }
    public UnitId[] EnemyUnitIds { get; set; }
    public int EnemyCharacterId { get; set; }
    public int EnemyAssetId { get; set; }
    public int MapId { get; set; }
    public int SessionId { get; set; }
    public int StageId { get; set; }
}

public class SinglePlayStartPacketResponse
{
    public bool SinglePlayStartOk { get; set; }
}

public class TutorialStartPacketRequired
{
    public int UserId { get; set; }
    public Faction UserFaction { get; set; }
    public UnitId[] UnitIds { get; set; }
    public int CharacterId { get; set; }
    public int AssetId { get; set; }
    public int EnemyCharacterId { get; set; }
    public int EnemyAssetId { get; set; }
    public int MapId { get; set; }
    public int SessionId { get; set; }
}

public class TutorialStartPacketResponse
{
    public bool TutorialStartOk { get; set; }
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

public class RankGameRewardPacketRequired
{
    public int WinUserId { get; set; }
    public int WinRankPoint { get; set; }
    public int LoseUserId { get; set; }
    public int LoseRankPoint { get; set; }
}

public class RankGameRewardPacketResponse
{
    public bool GetGameRewardOk { get; set; }
    public List<RewardInfo> WinnerRewards { get; set; }
    public List<RewardInfo> LoserRewards { get; set; }
}

public class SingleGameRewardPacketRequired
{
    public int UserId { get; set; }
    public int StageId { get; set; }
    public int Star { get; set; }
}

public class SingleGameRewardPacketResponse
{
    public bool GetGameRewardOk { get; set; }
    public List<SingleRewardInfo> Rewards { get; set; }
}

public class TutorialRewardPacketRequired
{
    public int UserId { get; set; }
    public Faction Faction { get; set; }
}

public class TutorialRewardPacketResponse
{
    public bool GetGameRewardOk { get; set; }
    public List<RewardInfo> Rewards { get; set; }
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
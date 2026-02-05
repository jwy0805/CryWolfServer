using Google.Protobuf.Protocol;

namespace Server.Game;

public interface ITutorialTrigger
{
    void TryTrigger(Player player, Faction faction, string tutorialTag, bool isInterrupted, Func<bool> condition);
    bool HasTriggered(string key);
}

public interface INetworkFactory
{
    Player CreatePlayer(GameRoom room, MatchSuccessPacketRequired required, Faction faction);
    Player CreatePlayerFriendly(GameRoom room, FriendlyMatchPacketRequired required, Faction faction);
    Player CreatePlayerSingle(GameRoom room, SinglePlayStartPacketRequired required);
    Player CreatePlayerTutorial(GameRoom room, TutorialStartPacketRequired required);
    Player CreateNpc(GameRoom room, Player player, CharacterId characterId, int assetId, UnitId[]? unitIds = null);
    void CreateNpcForAiGame(GameRoom room, Faction faction, CharacterId characterId, int assetId);
    void CreateNpcForAiGame(GameRoom room, Faction faction, int sessionId, CharacterId characterId, int assetId);
}

public interface IGameSetupHandler
{
    Task StartRankGame(MatchSuccessPacketRequired packet, DateTime? startTime = null);
    Task StartFriendlyGame(FriendlyMatchPacketRequired packet, DateTime? startTime = null);
    Task<bool> StartSingleGameAsync(SinglePlayStartPacketRequired packet);
    Task<bool> StartTutorialAsync(TutorialStartPacketRequired packet);
    Task<bool> SurrenderGameAsync(GameResultPacketRequired packet);
}
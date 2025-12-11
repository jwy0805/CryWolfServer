using Google.Protobuf.Protocol;

namespace Server.Game;

public interface ITutorialTrigger
{
    void TryTrigger(Player player, Faction faction, string tutorialTag, bool isInterrupted, Func<bool> condition);
    bool HasTriggered(string key);
}
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class TutorialTriggerService : ITutorialTrigger
{
    private readonly GameMode _gameMode;
    private readonly Dictionary<string, bool> _flags = new(StringComparer.OrdinalIgnoreCase);

    public TutorialTriggerService(GameMode gameMode)
    {
        _gameMode = gameMode;
    }
    
    public void TryTrigger(Player player, Faction faction, string tutorialTag, bool isInterrupted, Func<bool> condition)
    {
        // 튜토리얼 모드가 아니면 무시
        if (_gameMode != GameMode.Tutorial) return;
        if (player.Session == null) return;
        if (player.Faction != faction) return;
        if (string.IsNullOrEmpty(tutorialTag)) return;
        if (_flags.TryGetValue(tutorialTag, out var already) && already) return;
        if (!condition()) return;
        
        player.Session.Send(new S_RunTutorialTag
        {
            IsInterrupted = isInterrupted,
            TutorialTag = tutorialTag,
        });
        
        _flags[tutorialTag] = true;
    }

    public bool HasTriggered(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return _flags.TryGetValue(key, out var already) && already;
    }
}

public sealed class IgnoreTutorialTrigger : ITutorialTrigger
{
    public void TryTrigger(Player player, Faction faction, string tutorialTag, bool isInterrupted, Func<bool> condition)
    {
        // Try Nothing
    }

    public bool HasTriggered(string key) => false;
}
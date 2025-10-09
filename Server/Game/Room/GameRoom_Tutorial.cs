using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    private readonly Dictionary<string, bool> _tutorialTags = new()
    {
        { "UpkeepReminder", false },
    };
    
    private void ManageTutorial()
    {
        if (RoundTime >= 15 || GameMode != GameMode.Tutorial || TutorialSpawnFlag) return;
        _tutorialWaveModule?.Spawn(_round);
    }
}
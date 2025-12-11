using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    private void ManageTutorial()
    {
        var tutorialPlayer = _players.Values.FirstOrDefault(p => p.IsNpc == false);
        if (tutorialPlayer != null)
        {
            var faction = tutorialPlayer.Faction;
            if (faction == Faction.Wolf)
            {
                ManageTutorialUpkeepTracker(tutorialPlayer, _statueTracker);
            }
            else
            {
                ManageTutorialUpkeepTracker(tutorialPlayer, _towerTracker);
            }
        }
        
        ManageTutorialSpawn();
    }

    private void ManageTutorialUpkeepTracker<T>(Player player, UpkeepTracker<T> tracker) where T : GameObject
    {
        if (GameMode != GameMode.Tutorial) return;
        if (!tracker.HasAnyExcessThisRound) return;
        if (player.Session == null) return;
        
        _tutorialTrigger.TryTrigger(player, player.Faction,
            $"Battle{player.Faction}.InfoUpkeep",
            true,
            () => true
        );
    }
    
    private void ManageTutorialSpawn()
    {
        if (RoundTime >= 15 || GameMode != GameMode.Tutorial || TutorialSpawnFlag) return;
        _tutorialWaveModule?.Spawn(_round);
    }

    public void SheepResourceIncreasedFirst(Player player)
    {
        if (_tutorialTrigger.HasTriggered("BattleSheep.InfoResource")) return;
        _tutorialTrigger.TryTrigger(player, player.Faction,
            "BattleSheep.InfoResource",
            false,
            () => player.Faction == Faction.Sheep);
    }
}
namespace Server.Game;

public partial class GameRoom
{
    private void CheckMonsters()
    {
        if (_monsters.Values.Any(monster => monster.Targetable || monster.Hp > 0) == false)
        {
            MoveForwardTowerAndFence();
        }
    }

    private void MoveForwardTowerAndFence()
    {
        
    }
    
    private void InitRound()
    {
        
    }
}
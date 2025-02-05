using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage
{
    public GameRoom? Room { get; set; }
    
    public virtual void Spawn(int round)
    {
        
    }
}
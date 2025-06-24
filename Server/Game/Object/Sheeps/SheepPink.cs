namespace Server.Game;

public class SheepPink : Sheep
{
    public override void Init()
    {
        base.Init();
        if (Room != null)
        {
            YieldTime = Room.GameData.RoundTime / 5;
        }    
    }
}
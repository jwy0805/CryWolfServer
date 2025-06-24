namespace Server.Game;

public class SheepBlack : Sheep
{
    public override void Init()
    {
        base.Init();
        Stat.Hp += 40;
        Stat.MaxHp += 40;
    }
}
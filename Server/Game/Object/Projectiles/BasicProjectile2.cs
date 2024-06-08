namespace Server.Game;

public class BasicProjectile2 : Projectile
{
    public override void Init()
    {
        base.Init();
        MoveSpeed = 8f;
    }
}
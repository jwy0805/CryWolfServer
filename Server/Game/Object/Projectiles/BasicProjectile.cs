using Google.Protobuf.Protocol;

namespace Server.Game;

public class BasicProjectile : Projectile
{
    public override void Init()
    {
        base.Init();
        MoveSpeed = 8f;
    }
}
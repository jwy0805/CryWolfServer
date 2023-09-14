using Google.Protobuf.Protocol;

namespace Server.Game;

public class BigFire : Projectile
{
    public override void Init()
    {
        base.Init();
        ProjectileId = ProjectileId.BigFire;
    }
}
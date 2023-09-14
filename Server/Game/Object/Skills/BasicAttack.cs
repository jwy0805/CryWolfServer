using Google.Protobuf.Protocol;

namespace Server.Game;

public class BasicAttack : Projectile
{
    public override void Init()
    {
        base.Init();
        ProjectileId = ProjectileId.BasicAttack;
    }
}
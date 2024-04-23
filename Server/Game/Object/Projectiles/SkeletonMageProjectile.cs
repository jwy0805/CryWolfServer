using Google.Protobuf.Protocol;

namespace Server.Game;

public class SkeletonMageProjectile : Projectile
{
    public override void Init()
    {
        base.Init();
        ProjectileId = ProjectileId.SkeletonMageProjectile;
    }
}
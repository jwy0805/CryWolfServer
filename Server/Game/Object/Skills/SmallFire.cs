using Google.Protobuf.Protocol;

namespace Server.Game;

public class SmallFire : Projectile
{
    public override void Init()
    {
        base.Init();
        ProjectileId = ProjectileId.SmallFire;
    }
    public override void SetProjectileEffect(GameObject master)
    {
        BuffManager.Instance.AddBuff(BuffId.Burn, (Creature)master, Parent!.Attack);
    }
}
using Google.Protobuf.Protocol;

namespace Server.Game;

public class SmallFire : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        BuffManager.Instance.AddBuff(BuffId.Burn, (Creature)master, Parent!.Attack);
    }
}
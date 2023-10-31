using Google.Protobuf.Protocol;

namespace Server.Game;

public class SmallFire : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        BuffManager.Instance.AddBuff(BuffId.Burn, master, (Parent as Creature)!, Parent!.Attack);
    }
}
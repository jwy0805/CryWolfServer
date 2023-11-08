using Google.Protobuf.Protocol;

namespace Server.Game;

public class BigPoison : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        if (Target is Creature creature)
        {
            BuffManager.Instance.AddBuff(BuffId.DeadlyAddicted, creature, (Parent as Creature)!, Parent!.Attack);
        }
    }
}
using Google.Protobuf.Protocol;

namespace Server.Game;

public class SmallFire : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        if (Target is Creature creature)
        {
            BuffManager.Instance.AddBuff(BuffId.Burn, creature, Parent!.Attack);
        }
        else
        {
            if (Target == null) return;
            BuffManager.Instance.AddBuff(BuffId.Addicted, Target, Parent!.Attack);
        }
    }
}
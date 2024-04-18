using Google.Protobuf.Protocol;

namespace Server.Game;

public class SmallPoison : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        if (Target is Creature creature)
        {
            BuffManager.Instance.AddBuff(BuffId.Addicted, creature, (Parent as Creature)!, Parent!.Attack);
        }
    }
}
using Google.Protobuf.Protocol;

namespace Server.Game;

public class HauntFire : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        if (Parent is not Haunt haunt) return;
        
        if (Target is Creature creature)
        {
            BuffManager.Instance.AddBuff(BuffId.Burn, creature, haunt, 50);
        }
    }
}
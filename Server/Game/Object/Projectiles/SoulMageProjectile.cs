using Google.Protobuf.Protocol;

namespace Server.Game;

public class SoulMageProjectile : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        if (Parent is not SoulMage soulMage) return;
        
        if (Target is Creature creature && soulMage.Fire)
        {
            BuffManager.Instance.AddBuff(BuffId.Burn, creature, soulMage, 90);
        }
    }
}
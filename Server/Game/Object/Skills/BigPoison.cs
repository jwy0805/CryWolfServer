using Google.Protobuf.Protocol;

namespace Server.Game;

public class BigPoison : Projectile
{
    public override void Init()
    {
        base.Init();
        ProjectileId = ProjectileId.BigPoison;
    }
    
    public override void SetProjectileEffect(GameObject master)
    {
        if (Target is Creature creature)
        {
            BuffManager.Instance.AddBuff(BuffId.DeadlyAddicted, creature, Parent!.Attack);
        }
    }
}
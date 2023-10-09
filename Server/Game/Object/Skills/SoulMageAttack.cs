using Google.Protobuf.Protocol;

namespace Server.Game;

public class SoulMageAttack : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        if (Parent is not SoulMage soulMage) return;
        
        if (Target is Creature creature && soulMage.Fire == true)
        {
            BuffManager.Instance.AddBuff(BuffId.Addicted, creature, Parent!.Attack);
        }
    }
}
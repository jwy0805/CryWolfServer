using Google.Protobuf.Protocol;

namespace Server.Game;

public class BlossomProjectile : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        if (Parent is not Blossom blossom) return;

        if (Target is Creature creature && blossom.BlossomDeath)
        {
            Random random = new Random();
            int prob = random.Next(1, 100);
            if (prob < blossom.DeathProb) creature.OnDamaged(Parent, 9999, Damage.True);
        }
    }
}
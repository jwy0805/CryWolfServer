using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoStingerAttack : Projectile
{
    public override void SetProjectileEffect(GameObject master)
    {
        if (Target is Creature creature)
        {
            if (Parent is not MosquitoStinger mosquito) return;
            if (mosquito.Poison) 
                BuffManager.Instance.AddBuff(BuffId.Addicted, creature, mosquito, mosquito.Attack);
            if (creature is not Sheep sheep) return;

            sheep.YieldStop = true;
            if (mosquito.Infection) sheep.Infection = true;
            if (mosquito.SheepDeath)
            {
                if (sheep.Infection == false) return;
                Random random = new();
                if (random.Next(99) < mosquito.DeathRate) sheep.OnDead(this);
            }
        }
    }
}
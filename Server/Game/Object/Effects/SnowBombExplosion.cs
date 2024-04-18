using Google.Protobuf.Protocol;

namespace Server.Game;

public class SnowBombExplosion : Effect
{
    protected override void SetEffectEffect()
    {
        var targetList = new List<GameObjectType>
            { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var gameObjects = Room.FindTargets(this, targetList, Parent.SkillRange);
        foreach (var gameObject in gameObjects)
        {
            gameObject.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            if (Parent is SnowBomb { ExplosionBurn: true } snowBomb)
            {
                BuffManager.Instance.AddBuff(BuffId.Burn, gameObject, snowBomb, 0, 5);
            }
        }
        
        base.SetEffectEffect();
    }
}
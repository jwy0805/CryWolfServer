using Google.Protobuf.Protocol;

namespace Server.Game;

public class Effect : GameObject
{
    public EffectId EffectId;
    
    public override void Init()
    {
        EffectId = EffectId.NoEffect;
    }

    protected Effect()
    {
        ObjectType = GameObjectType.Effect;
    }

    public override void Update()
    {
        
    }

    public virtual void SetEffectEffect(GameObject master) { }
}
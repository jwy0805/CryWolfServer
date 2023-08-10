using Google.Protobuf.Protocol;

namespace Server.Game;

public class Effect : GameObject
{
    public virtual void Init()
    {
        
    }
    
    public Effect()
    {
        ObjectType = GameObjectType.Effect;
    }

    public override void Update()
    {
        
    }
}
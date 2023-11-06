using Google.Protobuf.Protocol;

namespace Server.Game;

public class Meteor : Effect
{
    public override void Update()
    {
        base.Update();
    }

    public override void SetEffectEffect()
    {
        base.SetEffectEffect();
    }
    
    public override PositionInfo SetEffectPos(GameObject parent)
    {
        List<GameObjectType> typeList = new() { GameObjectType.Tower };
        
        
        return parent.PosInfo;
    }
}
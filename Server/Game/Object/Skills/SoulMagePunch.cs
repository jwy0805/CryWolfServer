using Google.Protobuf.Protocol;

namespace Server.Game;

public class SoulMagePunch : Effect
{
    private bool _isHit = false;

    public override void Update()
    {
        
    }

    public override void SetEffectEffect(GameObject master)
    {
        if (PacketReceived == false || _isHit || Room == null) return;
        List<GameObjectType> typeList = new() { GameObjectType.Monster, GameObjectType.RockPile };
        List<GameObject> targets = Room.FindTargetsInRange(typeList, 4, 7);
        foreach (var VARIABLE in targets)
        {
            
        }

        _isHit = true;
    }
    
    public override PositionInfo SetEffectPos(GameObject parent)
    {
        return parent.PosInfo;
    }
}
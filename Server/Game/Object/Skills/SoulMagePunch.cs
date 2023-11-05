using Google.Protobuf.Protocol;

namespace Server.Game;

public class SoulMagePunch : Effect
{
    private bool _isHit = false;

    public override void Update()
    {
        base.Update();
        if (_isHit == false && PacketReceived) SetEffectEffect(Parent!);
    }

    public override void SetEffectEffect(GameObject master)
    {
        if (PacketReceived == false || _isHit || Room == null || Parent == null) return;
        List<GameObjectType> typeList = new() { GameObjectType.Monster, GameObjectType.RockPile };
        List<GameObject> targets = Room.FindTargetsInRectangle(typeList, this, 4, 7);
        foreach (var t in targets)
        { 
            t.OnDamaged(Parent, Parent.SkillDamage);
        }
        
        _isHit = true;
    }
    
    public override PositionInfo SetEffectPos(GameObject parent)
    {
        return parent.PosInfo;
    }
}
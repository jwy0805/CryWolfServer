using Google.Protobuf.Protocol;

namespace Server.Game;

public class SoulMagePunch : Effect
{
    private bool _isHit = false;

    public override void Update()
    {
        base.Update();
        if (_isHit == false && PacketReceived) SetEffectEffect();
    }

    public override void SetEffectEffect()
    {
        if (PacketReceived == false || _isHit || Room == null || Parent == null) return;
        List<GameObjectType> typeList = new() { GameObjectType.Monster, GameObjectType.RockPile };
        List<GameObject> targets = Room.FindTargetsInRectangle(typeList, this, 8, 24, AttackType);
        foreach (var t in targets) t.OnDamaged(Parent, Parent.SkillDamage);
        _isHit = true;
        base.SetEffectEffect();
    }
    
    public override PositionInfo SetEffectPos(GameObject parent)
    {
        return parent.PosInfo;
    }
}
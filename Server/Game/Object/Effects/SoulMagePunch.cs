using Google.Protobuf.Protocol;

namespace Server.Game;

public class SoulMagePunch : Effect
{
    protected override void SetEffectEffect()
    {
        if (PacketReceived == false || IsHit || Room == null || Parent == null) return;
        List<GameObjectType> typeList = new() { GameObjectType.Monster, GameObjectType.Portal };
        List<GameObject> targets = Room.FindTargetsInRectangle(typeList, this, 8, 24, AttackType);
        foreach (var t in targets) t.OnDamaged(Parent, Parent.SkillDamage, Damage.Magical);
        base.SetEffectEffect();
    }
    
    public override PositionInfo SetEffectPos(GameObject parent)
    {
        return parent.PosInfo;
    }
}
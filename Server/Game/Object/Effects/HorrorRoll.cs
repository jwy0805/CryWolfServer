using Google.Protobuf.Protocol;

namespace Server.Game;

public class HorrorRoll : Effect
{
    protected override void SetEffectEffect()
    {
        if (PacketReceived == false || IsHit || Room == null || Parent == null) return;
        List<GameObjectType> typeList = new() { GameObjectType.Monster, GameObjectType.RockPile };
        List<GameObject> targets = Room.FindTargetsInCone(typeList, this, 60, 8, AttackType);
        foreach (var t in targets) t.OnDamaged(Parent, Parent.SkillDamage);
        base.SetEffectEffect();
    }
    
    public override PositionInfo SetEffectPos(GameObject parent)
    {
        return parent.PosInfo;
    }
}
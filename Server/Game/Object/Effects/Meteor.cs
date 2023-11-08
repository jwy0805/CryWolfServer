using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Meteor : Effect
{
    private readonly float _meteorRad = 2.5f;

    protected override void SetEffectEffect()
    {
        if (Room == null || Parent == null || IsHit) return;
        List<GameObjectType> typeList = new() { GameObjectType.Tower, GameObjectType.Sheep, GameObjectType.Fence };
        List<GameObject> targets = Room.FindTargets(this, typeList, _meteorRad);
        foreach (var t in targets) t.OnDamaged(Parent, Parent.Attack); 
        
        base.SetEffectEffect();
    }
    
    public override PositionInfo SetEffectPos(GameObject parent)
    {
        if (Room == null) return parent.PosInfo;
        List<GameObjectType> typeList = new() { GameObjectType.Tower };
        List<GameObjectType> targetList = new() { GameObjectType.Tower, GameObjectType.Sheep, GameObjectType.Fence };
        GameObject? target = Room.FindTargetWithManyFriends(typeList, targetList, this, _meteorRad);

        if (target != null) return target.PosInfo;
        Vector3 v = GameData.Center;
        return new PositionInfo { PosX = v.X, PosY = v.Y, PosZ = v.Z };
    }
}
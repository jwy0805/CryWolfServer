using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonBelt : Effect
{
    public override void Update()
    {
        base.Update();
        CellPos = Parent!.CellPos;
    }

    public override void SetEffectEffect()
    {
        if (Room == null || Parent == null) return;
        List<GameObjectType> typeList = new() { GameObjectType.Tower, GameObjectType.Sheep, GameObjectType.Fence };
        List<GameObject> targets = Room.FindTargets(this, typeList, 6.0f);
        foreach (var t in targets)
        {
            t.OnDamaged(Parent, Parent.Attack);
            // 중독 버프 처리
        }
        base.SetEffectEffect();
    }
}
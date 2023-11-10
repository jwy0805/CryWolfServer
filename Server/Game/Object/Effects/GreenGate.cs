using Google.Protobuf.Protocol;

namespace Server.Game;

public class GreenGate : Effect
{
    private long _packetReceivedTime = 0;

    public override void Update()
    {
        base.Update();
        if (Room?.Stopwatch.ElapsedMilliseconds > _packetReceivedTime + 1000) Room?.LeaveGame(Id);
    }

    protected override void SetEffectEffect()
    {
        if (Room == null || Parent == null || IsHit) return;
        Random random = new();
        int rand = random.Next(3);
        switch (rand)
        {
            case 0:
                CreateEffect(EffectId.NaturalTornado);
                break;
            case 1:
                CreateEffect(EffectId.PurpleBeam);
                break;
            case 2:
                CreateEffect(EffectId.StarFall);
                break;
        }

        IsHit = true;
        _packetReceivedTime = Room.Stopwatch.ElapsedMilliseconds;
    }

    private void CreateEffect(EffectId effectId)
    {
        if (Room == null || Parent == null) return;
        Effect effect = ObjectManager.Instance.CreateEffect(effectId);
        effect.Room = Room;
        effect.Parent = Parent;
        effect.PosInfo = SetEffectPos(Parent);
        effect.Info.PosInfo = effect.PosInfo;
        effect.Info.Name = effectId.ToString();
        effect.Init();
        Room.EnterGame_Parent(effect, Parent);
    }
    
    public override PositionInfo SetEffectPos(GameObject master)
    {
        return master.PosInfo;
    }
}
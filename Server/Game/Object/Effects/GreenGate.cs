using Google.Protobuf.Protocol;

namespace Server.Game;

public class GreenGate : Effect
{
    private long _packetReceivedTime;

    public override void Update()
    {
        base.Update();
        if (IsHit && Room?.Stopwatch.ElapsedMilliseconds > _packetReceivedTime + 1000) Room?.LeaveGame(Id);
    }

    protected override void SetEffectEffect()
    {
        if (Room == null || Target == null || IsHit) return;
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
        // if (Room == null || Target == null || Parent == null) return;
        // Effect effect = ObjectManager.Instance.CreateEffect(effectId);
        // effect.Room = Room;
        // effect.Parent = Parent;
        // effect.Target = Target;
        // // effect.PosInfo = SetEffectPos(Target);
        // effect.Info.PosInfo = effect.PosInfo;
        // effect.Info.Name = effectId.ToString();
        // effect.Init();
        // Room.EnterGameTarget(effect, effect.Parent, effect.Target);
    }
}
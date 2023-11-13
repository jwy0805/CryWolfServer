using Google.Protobuf.Protocol;

namespace Server.Game;

public class StarFall : Effect
{
    private long _damageTime = 0;
    private readonly long _duration = 2000;
    private readonly float _starFallRad = 2f;

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (Target == null) Room?.LeaveGame(Id);
        // else CellPos = Parent.CellPos;
        
        if (Room?.Stopwatch.ElapsedMilliseconds > _damageTime + 500)
        {
            SetEffectEffect();
            _damageTime = Room.Stopwatch.ElapsedMilliseconds;
        }
    }

    protected override void SetEffectEffect()
    {
        if (Target == null || Room == null) return;
        int damage = 150;
        List<GameObjectType> typeList = new() { GameObjectType.Monster };
        List<GameObject> targets = Room.FindTargets(this, typeList, _starFallRad);
        foreach (var t in targets) t.OnDamaged(this, damage);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + _duration) Room.LeaveGame(Id);
    }
}
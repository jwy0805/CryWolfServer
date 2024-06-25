using Google.Protobuf.Protocol;

namespace Server.Game;

public class StarFall : Effect
{
    private long _damageTime = 0;
    private readonly long _duration = 2500;
    private readonly long _interval = 500;
    private readonly float _starFallRad = 2f;
    private readonly int _skillDamage = 120;

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (Room?.Stopwatch.ElapsedMilliseconds > _damageTime + _interval)
        {
            _damageTime = Room.Stopwatch.ElapsedMilliseconds;
        }
    }

    // protected override void SetEffectEffect()
    // {
    //     if (Room == null) return;
    //     if (Room.Stopwatch.ElapsedMilliseconds > Time + _duration)
    //     {
    //         Room.LeaveGame(Id);
    //         return;
    //     }
    //     
    //     List<GameObjectType> typeList = new() { GameObjectType.Monster };
    //     List<GameObject> targets = Room.FindTargets(this, typeList, _starFallRad);
    //     if (!targets.Any()) return;
    //     foreach (var t in targets) t.OnDamaged(this, _skillDamage, Damage.Magical);
    // }
}
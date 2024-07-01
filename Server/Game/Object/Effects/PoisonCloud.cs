using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonCloud : Effect
{
    public override void Init()
    {
        base.Init();
        EffectImpact(300);
        EffectImpact(1300);
        EffectImpact(2300);
    }
    
    protected override async void EffectImpact(long impactTime)
    {   // parent가 사라져도 이펙트 효과가 발동되도록 하기 위해 클래스 내에서 효과 직접 발동
        if (Room == null) return; 
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Room == null) return;
            var types = new[] { GameObjectType.Monster };
            var targets = Room.FindTargets(CellPos, types, 3, 2);
            var parent = (Toadstool)Parent!;
            foreach (var target in targets)
            {
                BuffManager.Instance.AddBuff(BuffId.Addicted, BuffParamType.Percentage,
                    target, parent, 0.03f, 5000, parent.NestedPoison);
            }
        });
    }
}
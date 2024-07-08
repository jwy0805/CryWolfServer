using System.Numerics;
using System.Threading.Channels;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MothLuna : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothLunaAccuracy:
                    Accuracy += 15;
                    break;
                case Skill.MothLunaRange:
                    AttackRange += 1;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Supporter;
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.BasicProjectile, this, 5f);
        });
    }
}
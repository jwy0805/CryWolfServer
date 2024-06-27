using Google.Protobuf.Protocol;

namespace Server.Game;

public sealed partial class BuffManager
{
    public interface IBuffFactory
    {
        Buff CreateBuff();
    }

    public class AttackBuffFactory : IBuffFactory { public Buff CreateBuff() => new AttackBuff(); }
    public class AttackSpeedBuffFactory : IBuffFactory { public Buff CreateBuff() => new AttackSpeedBuff(); }
    public class HealBuffFactory : IBuffFactory { public Buff CreateBuff() => new HealBuff(); }
    public class HealthBuffFactory : IBuffFactory { public Buff CreateBuff() => new HealthBuff(); }
    public class DefenceBuffFactory : IBuffFactory { public Buff CreateBuff() => new DefenceBuff(); }
    public class MoveSpeedBuffFactory : IBuffFactory { public Buff CreateBuff() => new MoveSpeedBuff(); }
    public class InvincibleFactory : IBuffFactory { public Buff CreateBuff() => new Invincible(); }
    public class AttackDebuffFactory : IBuffFactory { public Buff CreateBuff() => new AttackDebuff(); }
    public class AttackSpeedDebuffFactory : IBuffFactory { public Buff CreateBuff() => new AttackSpeedDebuff(); }
    public class DefenceDebuffFactory : IBuffFactory { public Buff CreateBuff() => new DefenceDebuff(); }
    public class MoveSpeedDebuffFactory : IBuffFactory { public Buff CreateBuff() => new MoveSpeedDebuff(); }
    public class CurseFactory : IBuffFactory { public Buff CreateBuff() => new Curse(); }
    public class AddictedFactory : IBuffFactory { public Buff CreateBuff() => new Addicted(); }
    public class AggroFactory : IBuffFactory { public Buff CreateBuff() => new Aggro(); }
    public class BurnFactory : IBuffFactory { public Buff CreateBuff() => new Burn(); }
    public class FaintedFactory : IBuffFactory { public Buff CreateBuff() => new Fainted(); }
}
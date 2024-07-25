using System.Diagnostics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public sealed class BuffManager
{
    public static BuffManager Instance { get; } = new();
    
    public readonly Dictionary<BuffId, IBuffFactory> BuffDict = new() 
    {
        { BuffId.AttackBuff, new AttackBuffFactory() },
        { BuffId.AttackSpeedBuff, new AttackSpeedBuffFactory() },
        { BuffId.HealBuff, new HealBuffFactory() },
        { BuffId.HealthBuff, new HealthBuffFactory() },
        { BuffId.DefenceBuff, new DefenceBuffFactory() },
        { BuffId.MoveSpeedBuff, new MoveSpeedBuffFactory() },
        { BuffId.Invincible, new InvincibleFactory() },
        { BuffId.AttackDebuff, new AttackDebuffFactory() },
        { BuffId.AttackSpeedDebuff, new AttackSpeedDebuffFactory() },
        { BuffId.DefenceDebuff, new DefenceDebuffFactory() },
        { BuffId.MoveSpeedDebuff, new MoveSpeedDebuffFactory() },
        { BuffId.Curse, new CurseFactory() },
        { BuffId.Addicted, new AddictedFactory() },
        { BuffId.Aggro, new AggroFactory() },
        { BuffId.Burn, new BurnFactory() },
        { BuffId.Fainted, new FaintedFactory() }
    };
    
    public interface IBuffFactory
    {
        Buff CreateBuff();
    }

    private class AttackBuffFactory : IBuffFactory { public Buff CreateBuff() => new AttackBuff(); }
    private class AttackSpeedBuffFactory : IBuffFactory { public Buff CreateBuff() => new AttackSpeedBuff(); }
    private class HealBuffFactory : IBuffFactory { public Buff CreateBuff() => new HealBuff(); }
    private class HealthBuffFactory : IBuffFactory { public Buff CreateBuff() => new HealthBuff(); }
    private class DefenceBuffFactory : IBuffFactory { public Buff CreateBuff() => new DefenceBuff(); }
    private class MoveSpeedBuffFactory : IBuffFactory { public Buff CreateBuff() => new MoveSpeedBuff(); }
    private class InvincibleFactory : IBuffFactory { public Buff CreateBuff() => new Invincible(); }
    private class AttackDebuffFactory : IBuffFactory { public Buff CreateBuff() => new AttackDebuff(); }
    private class AttackSpeedDebuffFactory : IBuffFactory { public Buff CreateBuff() => new AttackSpeedDebuff(); }
    private class DefenceDebuffFactory : IBuffFactory { public Buff CreateBuff() => new DefenceDebuff(); }
    private class MoveSpeedDebuffFactory : IBuffFactory { public Buff CreateBuff() => new MoveSpeedDebuff(); }
    private class CurseFactory : IBuffFactory { public Buff CreateBuff() => new Curse(); }
    private class AddictedFactory : IBuffFactory { public Buff CreateBuff() => new Addicted(); }
    private class AggroFactory : IBuffFactory { public Buff CreateBuff() => new Aggro(); }
    private class BurnFactory : IBuffFactory { public Buff CreateBuff() => new Burn(); }
    private class FaintedFactory : IBuffFactory { public Buff CreateBuff() => new Fainted(); }
}


using Google.Protobuf.Protocol;

namespace Server.Game;

public sealed partial class BuffManager
{
    public interface IBuffFactory
    {
        ABuff CreateBuff();
    }

    public class AttackBuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new AttackBuff();
    }

    public class AttackSpeedBuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new AttackSpeedBuff();
    }

    public class HealBuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new HealBuff();
    }
    
    public class HealthBuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new HealthBuff();
    }
    
    public class DefenceBuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new DefenceBuff();
    }

    public class MoveSpeedBuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new MoveSpeedBuff();
    }
    
    public class InvincibleFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new Invincible();
    }

    public class AttackDebuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new AttackDebuff();
    }
    
    public class AttackSpeedDebuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new AttackSpeedDebuff();
    }
    
    public class DefenceDebuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new DefenceDebuff();
    }
    
    public class MoveSpeedDebuffFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new MoveSpeedDebuff();
    }
    
    public class CurseFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new Curse();
    }
    
    public class AddictedFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new Addicted();
    }

    public class DeadlyAddictedFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new DeadlyAddicted();
    }

    public class AggroFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new Aggro();
    }

    public class BurnFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new Burn();
    }
    
    public class FaintedFactory : IBuffFactory
    {
        public ABuff CreateBuff() => new Fainted();
    }
}
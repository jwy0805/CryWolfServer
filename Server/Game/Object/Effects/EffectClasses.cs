namespace Server.Game;

public class Meteor : Effect
{
    public override void Init()
    {
        base.Init();
        EffectImpact(500);
    }
}

public class PoisonBombExplosion : Effect { }
public class PoisonBombSkillExplosion : Effect{ }
public class SnowBombExplosion : Effect { }
public class BombSkillExplosion : Effect { }
public class StateHeal : Effect { }
public class StatePoison : Effect { }
public class StateAggro : Effect { }
public class StateBurn : Effect { }
public class StateCurse : Effect { }
public class StateDebuffRemove : Effect { }
public class StateFaint : Effect { }
public class StateSlow : Effect { }
public class UpgradeEffect : Effect { }
public class WolfMagicalEffect : Effect { }
public class WerewolfMagicalEffect : Effect { }
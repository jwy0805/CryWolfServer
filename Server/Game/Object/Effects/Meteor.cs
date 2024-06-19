namespace Server.Game;

public class Meteor : Effect
{
    public override void Init()
    {
        base.Init();
        EffectImpact(500);
    }
}
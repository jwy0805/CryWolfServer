using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Meteor : Effect
{
    public override void Init()
    {
        base.Init();
        EffectImpact(500);
    }
}
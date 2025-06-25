using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Spike : Shell
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SpikeReflection:
                    Reflection = true;
                    break;
                case Skill.SpikeDefence:
                    Defence += 10;
                    break;
                case Skill.SpikeFireResist:
                    FireResist += 25;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
    }
}
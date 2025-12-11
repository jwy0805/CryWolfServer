using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
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
                case Skill.SpikeMagicalDefence:
                    Defence += (int)DataManager.SkillDict[(int)Skill].Value;;
                    break;
                case Skill.SpikeFireResist:
                    FireResist += (int)DataManager.SkillDict[(int)Skill].Value;
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
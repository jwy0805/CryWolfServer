using Google.Protobuf.Protocol;

namespace Server.Game;

public class Hermit : Spike
{
    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.Hermit;
    }

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                
            }
        }
    }
}
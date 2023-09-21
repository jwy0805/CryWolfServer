using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoBug : Monster
{
    private bool _woolDown = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoBugAvoid:
                    Evasion += 10;
                    break;
                case Skill.MosquitoBugDefence:
                    Defence += 2;
                    break;
                case Skill.MosquitoBugSpeed:
                    MoveSpeed += 1.0f;
                    break;
                case Skill.MosquitoBugWoolDown:
                    _woolDown = true;
                    break;
            }
        }
    }
}
using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoPester : MosquitoBug
{
    private bool _woolDown = false;
    private bool _woolRate = false;
    private bool _woolStop = false;
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoPesterAttack:
                    Attack += 4;
                    break;
                case Skill.MosquitoPesterHealth:
                    MaxHp += 25;
                    Hp += 25;
                    break;
                case Skill.MosquitoPesterWoolDown2:
                    _woolDown = true;
                    break;
                case Skill.MosquitoPesterWoolRate:
                    _woolRate = true;
                    break;
                case Skill.MosquitoPesterWoolStop:
                    _woolStop = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.MosquitoPester;
    }
}
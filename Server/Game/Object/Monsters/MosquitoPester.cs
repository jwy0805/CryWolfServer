using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoPester : MosquitoBug
{
    private bool _woolProduceStop = false;
    private int _woolProduceRate = 25;
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
                    WoolDownRate = 30;
                    break;
                case Skill.MosquitoPesterWoolRate:
                    _woolProduceStop = true;
                    break;
                case Skill.MosquitoPesterWoolStop:
                    _woolStop = true;
                    break;
            }
        }
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        if (target is Sheep sheep)
        {
            sheep.YieldDecrement = sheep.Resource * WoolDownRate / 100;
            
            if (_woolProduceStop)
            {           
                Random random = new Random();
                if (random.Next(99) < _woolProduceRate) sheep.YieldStop = true;
            }
            
            if (_woolStop) sheep.YieldStop = true;
            
        }
    }
}
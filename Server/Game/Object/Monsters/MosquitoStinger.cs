using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MosquitoStinger : MosquitoPester
{
    private bool _woolStop = false;
    private bool _sheepDeath = false;
    private bool _infection = false;
    private readonly float _deathRate = 25;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoStingerWoolStop:
                    _woolStop = true;
                    break;
                case Skill.MosquitoStingerInfection:
                    _infection = true;
                    break;
                case Skill.MosquitoStingerSheepDeath:
                    _sheepDeath = true;
                    break;
            }
        }
    }
    
    public override void SetNormalAttackEffect(GameObject target)
    {
        BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0, 5);

        if (target is not Sheep sheep) return;
        
        Random random = new();
        if (_sheepDeath && random.Next(100) < _deathRate)
        {
            sheep.OnDamaged(this, 9999, Damage.True);
            return;
        }
            
        if (_infection) sheep.Infection = true;
        if (_woolStop) sheep.YieldStop = true;
        else sheep.YieldDecrement = sheep.Resource * WoolDownRate / 100;
        BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1);
    }
}
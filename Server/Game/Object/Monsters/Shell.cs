using Google.Protobuf.Protocol;

namespace Server.Game;

public class Shell : Monster
{
    private bool _speedBuff = false;
    private bool _attackSpeedBuff = false;
    private bool _roll = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.ShellHealth:
                    MaxHp += 35;
                    Hp += 35;
                    break;
                case Skill.ShellSpeed:
                    _speedBuff = true;
                    break;
                case Skill.ShellAttackSpeed:
                    _attackSpeedBuff = true;
                    break;
                case Skill.ShellRoll:
                    _roll = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.Shell;
    }
    
    
}
using Google.Protobuf.Protocol;

namespace Server.Game;

public class Bomb : Monster
{
    private bool _bombSkill = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BombHealth:
                    MaxHp += 25;
                    Hp += 25;
                    BroadcastHealth();
                    break;
                case Skill.BombAttack:
                    Attack += 4;
                    break;
                case Skill.BombBomb:
                    _bombSkill = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
        }

        if (_bombSkill && Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastPos();
            UpdateSkill();
            Mp = 0;
        }
        else
        {
            switch (State)
            {
                case State.Die:
                    UpdateDie();
                    break;
                case State.Moving:
                    UpdateMoving();
                    break;
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Skill:
                    UpdateSkill();
                    break;
                case State.KnockBack:
                    UpdateKnockBack();
                    break;
                case State.Faint:
                    break;
                case State.Standby:
                    break;
            }   
        }
    }

    // public override void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    // {
    //     if (pId != ProjectileId.BombSkill) return;
    //     target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
    // }
}
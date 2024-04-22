using Google.Protobuf.Protocol;

namespace Server.Game;

public class MoleRat : Burrow
{
    private bool _burrowSpeed = false;
    private bool _burrowEvasion = false;
    private bool _drain = false;
    private bool _stealAttack = false;
    private bool _speedIncrease = false;
    private bool _evasionIncrease = false;
    
    protected readonly float DrainParam = 0.15f;
    protected readonly float StealAttackParam = 0.15f;
    protected int StolenObjectId;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MoleRatBurrowSpeed:
                    _burrowSpeed = true;
                    break;
                case Skill.MoleRatBurrowEvasion:
                    _burrowEvasion = true;
                    break;
                case Skill.MoleRatDrain:
                    _drain = true;
                    break;
                case Skill.MoleRatStealAttack:
                    _stealAttack = true;
                    break;
            }
        }
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this);
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        if (Target == null) return;
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        
        (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
        BroadcastDest();

        State = State.IdleToRush;
        BroadcastPos();
    }

    public override void SetNextState(State state)
    {
        if (state == State.IdleToRush)
        {
            State = State.Rush;
            BroadcastPos();
            
            if (_burrowSpeed) MoveSpeedParam += 2;
            if (_burrowEvasion) EvasionParam += 30;
            
        }

        if (state == State.RushToIdle)
        {
            State = State.Attack;
            BroadcastPos();
            
            if (_burrowSpeed) MoveSpeedParam -= 2;
            if (_burrowEvasion) EvasionParam -= 30;
        }
    }
    
    public override void SetNormalAttackEffect(GameObject target)
    {
        base.SetNormalAttackEffect(target);
        if (_drain) Hp += (int)((TotalAttack - target.TotalDefence) * DrainParam);
        if (_stealAttack == false) return;
        if (StolenObjectId == target.Id) return;
        StolenObjectId = target.Id;
        Attack += (int)(target.Attack * StealAttackParam);
        target.Attack -= Attack;
    }
}
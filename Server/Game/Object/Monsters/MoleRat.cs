using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class MoleRat : Burrow
{
    private bool _burrowSpeed = false;
    private bool _burrowEvasion = false;
    private bool _drain = false;
    private bool _stealAttack = false;

    protected readonly float BurrowSpeedParam = DataManager.SkillDict[(int)Skill.MoleRatBurrowSpeed].Value;
    protected int BurrowEvasionParam = (int)DataManager.SkillDict[(int)Skill.MoleRatBurrowEvasion].Value;
    protected readonly float DrainParam = DataManager.SkillDict[(int)Skill.MoleRatDrain].Value;
    protected readonly float StealAttackParam = DataManager.SkillDict[(int)Skill.MoleRatStealAttack].Value;
    protected int StolenObjectId;
    protected int StolenDamage;
    
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

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Warrior;
        IdleToRushAnimTime = StdAnimTime * 2 / 3;
        RushToIdleAnimTime = StdAnimTime * 5 / 6;
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this);
        if (Target == null) return;
        State = State.IdleToRush;
    }

    public override void SetNextState(State state)
    {
        if (state == State.Die && WillRevive)
        {
            State = State.Idle;
            Hp = (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHp();
            // 부활 Effect 추가
        }
        
        if (state == State.IdleToRush)
        {
            State = State.Rush;
            if (_burrowSpeed && StateChanged) MoveSpeedParam += BurrowSpeedParam;
            if (_burrowEvasion && StateChanged) EvasionParam += BurrowEvasionParam;
        }

        if (state == State.RushToIdle)
        {
            State = State.Attack;
            SyncPosAndDir();
            if (_burrowSpeed && StateChanged) MoveSpeedParam -= BurrowSpeedParam;
            if (_burrowEvasion && StateChanged) EvasionParam -= BurrowEvasionParam;
        }
    }
    
    public override void ApplyAttackEffect(GameObject target)
    {
        if (Room == null) return;
        var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
        if (_drain) Hp += (int)(damage * DrainParam);
        BroadcastHp();
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        Room.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.MonsterAttack, SoundType = SoundType.D3 });
        
        // Steal Attack
        if (_stealAttack == false || target.Targetable == false || target.Hp < 0) return;
        if (StolenObjectId == 0)
        {
            StolenDamage = (int)(target.TotalAttack * StealAttackParam);
            AttackParam += StolenDamage;
            target.AttackParam -= AttackParam;
            StolenObjectId = target.Id;
            return;
        }
        
        // Restore stolen attack when the target changed.
        if (StolenObjectId == target.Id) return;
        var stolenTarget = Room?.FindGameObjectById(StolenObjectId);
        if (stolenTarget == null) return;
        stolenTarget.AttackParam += StolenDamage;
        
        StolenDamage = (int)(target.TotalAttack * StealAttackParam);
        target.AttackParam -= StolenDamage;
        
        StolenObjectId = target.Id;
    }
}
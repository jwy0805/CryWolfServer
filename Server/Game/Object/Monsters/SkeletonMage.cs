using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SkeletonMage : SkeletonGiant
{
    private bool _adjacentRevive = false;
    private bool _killRecoverMp = false;
    private bool _reviveHealthUp = false;
    private bool _curse = false;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SkeletonMageAdjacentRevive:
                    _adjacentRevive = true;
                    break;
                case Skill.SkeletonMageKillRecoverMp:
                    _killRecoverMp = true;
                    break;
                case Skill.SkeletonMageReviveHealthUp:
                    _reviveHealthUp = true;
                    ReviveHpRate = 0.6f;
                    break;
                case Skill.SkeletonMageCurse:
                    _curse = true;
                    break;
            }
        }
    }

    public override int KillLog
    {
        get => base.KillLog;
        set
        {
            base.KillLog = value;
            if (_killRecoverMp == false) return;
            Mp += 25;
            if (Mp > MaxMp) Mp = MaxMp;
        }
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

        if (Mp >= MaxMp)
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
                case State.Revive:
                    UpdateRevive();
                    break;
                case State.Faint:
                    break;
                case State.Standby:
                    break;
            }
        }
    }

    public override void SetNormalAttackEffect(GameObject target) { }

    public override void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
    
    public override void SetNextState(State state)
    {
        if (state == State.Die)
        {
            if (AlreadyRevived == false || WillRevive)
            {
                AlreadyRevived = true;
                State = State.Revive;
                BroadcastPos();
                return;
            }
        }
        
        if (state == State.Revive)
        {
            State = State.Idle;
            Hp += (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHealth();
            BroadcastPos();
        }
    }
    
    public override void OnDead(GameObject attacker)
    {
        if (Room == null) return;
        attacker.KillLog = Id;
        Targetable = false;
        
        if (attacker.Target != null)
        {
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
            {
                if (attacker.Parent != null)
                {
                    attacker.Parent.Target = null;
                    attacker.State = State.Idle;
                    // BroadcastPos();
                }
            }
            attacker.Target = null;
            attacker.State = State.Idle;
            // BroadcastPos();
        }
        
        if (AlreadyRevived == false) return;
        S_Die diePacket = new() { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
    }

    public override void RunSkill()
    {
        var effect = Room.EnterEffect(EffectId.SkeletonGiantSkill, this, Target?.PosInfo);
        Room.EnterGameParent(effect, Target ?? this);
        
        var enemyTypeList = new HashSet<GameObjectType>
            { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
        var allyTypeList = new HashSet<GameObjectType> { GameObjectType.Monster };
        var curseTypeList = new HashSet<GameObjectType> { GameObjectType.Tower };
        
        var targets = Room.FindTargets(this, enemyTypeList, DebuffRange);
        foreach (var target in targets) target.DefenceParam -= DefenceDebuffParam;
        
        foreach (var target in targets)
        {
            BuffManager.Instance.AddBuff(BuffId.AttackDecrease, target, this, 2, 5000, true);
            BuffManager.Instance.AddBuff(BuffId.AttackIncrease, this, this, 2, 5000, true);
        }

        if (_adjacentRevive)
        {
            var reviveTargets = Room.FindTargets(
                this, allyTypeList, TotalSkillRange).Cast<Creature>().ToList();
            if (reviveTargets.Any())
            {
                foreach (var creature in reviveTargets.OrderBy(_ => Guid.NewGuid()).Take(1))
                {
                    creature.WillRevive = true;
                    if (_reviveHealthUp) creature.ReviveHpRate = 0.6f;
                }
            }
        }

        if (_curse)
        {
            var curseTargets = Room.FindTargets(
                this, curseTypeList, TotalSkillRange).Cast<Creature>().ToList();
            if (curseTargets.Any())
            {
                foreach (var creature in curseTargets.OrderBy(_ => Guid.NewGuid()).Take(1))
                {
                    BuffManager.Instance.AddBuff(BuffId.Curse, creature, this, 0, 5000);
                }
            }
        }
    }
}
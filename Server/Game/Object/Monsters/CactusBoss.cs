using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class CactusBoss : Cactus
{
    private bool _rush = false;
    private bool _smash = false;
    private bool _smashHeal = false;
    private bool _smashAggro = false;
    private bool _start = false;
    private bool _speedRestore = false;
    private bool _firstAttack = false;
    private readonly int _healParam = 100;
    private readonly int _smashDamage = 70;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CactusBossRush:
                    _rush = true;
                    break;
                case Skill.CactusBossSmash:
                    _smash = true;
                    break;
                case Skill.CactusBossSmashHeal:
                    _smashHeal = true;
                    break;
                case Skill.CactusBossSmashAggro:
                    _smashAggro = true;
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
        
        if (_rush && _start == false)
        {
            _start = true;
            State = State.Rush; 
        }
        else
        {
            State = State.Moving;
        }
        BroadcastPos();
    }

    protected override void UpdateMoving()
    {
        if (_rush && _start == false)
        {
            _start = true;
            MoveSpeedParam += 2;
            State = State.Rush;
            BroadcastPos();
            return;
        }
        
        if (_rush && _start && _speedRestore == false)
        {
            MoveSpeedParam -= 2;
            _speedRestore = true;
            BroadcastDest();
        }
        
        base.UpdateMoving();
    }

    protected override void UpdateRush()
    {
        // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target != null)
        {   
            // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
            Vector3 position = CellPos;
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
            double deltaX = DestPos.X - CellPos.X;
            double deltaZ = DestPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= AttackRange)
            {   // Attack3 = SMASH Animation
                MoveSpeedParam -= 2;
                CellPos = position;
                State = State.Attack3;    
                BroadcastPos();
                return;
            }
            
            // Target이 있으면 이동
            DestPos = Room.Map.GetClosestPoint(CellPos, Target);
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos, false);
            BroadcastDest();
        }
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            MoveSpeedParam -= 2;
            State = State.Idle;
            BroadcastPos();
        }
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }

        if (Target.Hp <= 0)
        {
            Target = null;
            State = State.Idle;
            BroadcastPos();
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));

        if (distance > TotalAttackRange)
        {
            DestPos = targetPos;
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            State = State.Moving;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
            return;
        }

        State = new Random().Next(2) == 0 ? State.Attack : State.Attack2;
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;

        int totalDamage;
        if (damageType is Damage.Normal or Damage.Magical)
        {
            totalDamage = attacker.CriticalChance > 0 
                ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
                : Math.Max(damage - TotalDefence, 0);
            if (damageType is Damage.Normal && Reflection && reflected == false)
            {
                int refParam = (int)(totalDamage * ReflectionRate);
                attacker.OnDamaged(this, refParam, damageType, true);
                var random = new Random();
                if (random.Next(99) >= ReflectionFaintRate) return;
                BuffManager.Instance.AddBuff(BuffId.Fainted, attacker, this, 0, 1000);
            }
        }
        else
        {
            totalDamage = damage;
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp <= 0) OnDead(attacker);
    }
    
    public override void SetAdditionalAttackEffect(GameObject target)
    {
       if (_firstAttack == false)
       {
           _firstAttack = true;
           target.OnDamaged(this, _smashDamage, Damage.Normal);
       }
       else
       {
           target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
           if (_smashHeal)
           {
               Hp += _healParam;
               Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
           }
           if (_smashAggro)
           {
               var towers = Room.FindTargets(this, new List<GameObjectType> { GameObjectType.Tower }, SkillRange);
               foreach (var tower in towers)
               {
                   BuffManager.Instance.AddBuff(BuffId.Aggro, tower, this, 0, 2000);
               } 
           }
       }

       Mp += 5;
    }
}
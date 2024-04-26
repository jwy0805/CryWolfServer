using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Creeper : Lurker
{
    private bool _roll = false;
    private bool _poison = false;
    private bool _nestedPoison = false;
    private bool _rollDamageUp = false;
    
    protected bool Start = false;
    protected bool SpeedRestore = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CreeperPoison:
                    _poison = true;
                    Room?.Broadcast(new S_SkillUpdate
                    {
                        ObjectEnumId = (int)UnitId,
                        ObjectType = GameObjectType.Monster,
                        SkillType = SkillType.SkillProjectile
                    });
                    break;
                case Skill.CreeperRoll:
                    _roll = true;
                    break;
                case Skill.CreeperNestedPoison:
                    _nestedPoison = true;
                    break;
                case Skill.CreeperRollDamageUp:
                    _rollDamageUp = true;
                    break;
            }
        }
    }
    
    protected override void UpdateMoving()
    {
        if (_roll && Start == false)
        {
            Start = true;
            MoveSpeedParam += 2;
            BroadcastDest();
            State = State.Rush;
            BroadcastPos();
            return;
        }
        
        if (_roll && Start && SpeedRestore == false)
        {
            MoveSpeedParam -= 2;
            SpeedRestore = true;
            BroadcastDest();
        }
        
        base.UpdateMoving();
    }

    protected override void UpdateRush()
    {
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }
        
        if (Room != null)
        {
            // 이동
            // target이랑 너무 가까운 경우
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                // Roll 충돌 처리
                if (distance <= Stat.SizeX * 0.25 + 0.75f)
                {
                    CellPos = position;
                    SetRollEffect(Target);
                    Mp += MpRecovery;
                    State = State.KnockBack;
                    DestPos = CellPos + (-Vector3.Normalize(Target.CellPos - CellPos) * 3);
                    BroadcastPos();
                    Room.Broadcast(new S_SetKnockBack
                    {
                        ObjectId = Id, 
                        Dest = new DestVector { X = DestPos.X, Y = DestPos.Y, Z = DestPos.Z }
                    });
                    return;
                }
            }
        }

        BroadcastPos();
    }

    protected virtual void SetRollEffect(GameObject target)
    {
        if (_rollDamageUp)
        {
            target.OnDamaged(this, TotalSkillDamage * 2, Damage.Normal);
        }
        else
        {
            target.OnDamaged(this, TotalSkillDamage, Damage.Normal);
        }
    }

    protected override void UpdateKnockBack()
    {
        // 넉백중 충돌하면 Idle
        //
    }

    public override void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        if (_poison)
        {
            BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0, 5000);
        }
        else if (_nestedPoison)
        {
            BuffManager.Instance.AddBuff(BuffId.DeadlyAddicted, target, this, 0, 5000);

        }
    }
}
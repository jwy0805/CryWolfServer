using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Blossom : Bloom
{
    public bool BlossomDeath = false;
    public readonly int DeathProb = 3;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BlossomPoison:
                    Room?.Broadcast(new S_SkillUpdate
                    {
                        ObjectEnumId = (int)TowerId,
                        ObjectType = GameObjectType.Tower,
                        SkillType = SkillType.SkillProjectile,
                    });
                    break;
                case Skill.BlossomAccuracy:
                    Accuracy += 20;
                    break;
                case Skill.BlossomAttack:
                    Attack += 20;
                    break;
                case Skill.BlossomAttackSpeed:
                    AttackSpeed += 0.2f;
                    break;
                case Skill.BlossomRange:
                    AttackRange += 3.0f;
                    break;
                case Skill.BlossomDeath:
                    BlossomDeath = true;
                    break;
            }
        }
    }

    protected override void UpdateIdle()
    {
        GameObject? target = Room?.FindNearestTarget(this);
        if (target == null) return;
        LastSearch = Room!.Stopwatch.ElapsedMilliseconds;
        Target ??= target;

        StatInfo targetStat = Target.Stat;
        if (targetStat.Targetable)
        {
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
            double deltaX = Target.CellPos.X - CellPos.X;
            double deltaZ = Target.CellPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= AttackRange)
            {
                State = State.Attack;
                BroadcastMove();
            }
        }
    }
    
    
}
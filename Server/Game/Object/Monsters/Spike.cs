using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Spike : Shell
{
    private bool _lostHeal = false;
    private bool _attackBuff = false;
    private bool _defenceBuff = false;
    private bool _doubleBuff = false;
    
    protected readonly float AttackBuffParam = 2.0f;
    protected readonly int DefenceBuffParam = 6;
    protected readonly float LostHealParam = 0.3f;
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SpikeSelfDefence:
                    Defence += 10;
                    break;
                case Skill.SpikeLostHeal:
                    _lostHeal = true;
                    break;
                case Skill.SpikeAttack:
                    _attackBuff = true;
                    break;
                case Skill.SpikeDefence:
                    _defenceBuff = true;
                    break;
                case Skill.SpikeDoubleBuff:
                    _doubleBuff = true;
                    break;
            }
        }
    }

    protected override void UpdateRush()
    {
        // Targeting
        double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
        if (timeNow > LastSearch + SearchTick)
        {
            LastSearch = timeNow;
            GameObject? target = Room?.FindNearestTarget(this);
            if (Target?.Id != target?.Id)
            {
                Target = target;
                if (Target != null)
                {
                    DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                    (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                }
            }
        }
        
        if (Target == null || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
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
                    CrashTime = Room.Stopwatch.ElapsedMilliseconds;
                    Target.OnDamaged(this, SkillDamage);
                    if (_lostHeal) Hp += (int)((MaxHp - Hp) * LostHealParam);
                    Mp += MpRecovery;
                    State = State.KnockBack;
                    DestPos = CellPos + (-Vector3.Normalize(Target.CellPos - CellPos) * 3);
                    BroadcastMove();
                    Room.Broadcast(new S_SetKnockBack
                    {
                        ObjectId = Id, 
                        Dest = new DestVector { X = DestPos.X, Y = DestPos.Y, Z = DestPos.Z }
                    });
                    return;
                }
            }
        }

        BroadcastMove();
    }
    
    public override void RunSkill()
    {
        if (Room == null) return;
        int num = _doubleBuff ? 2 : 1;
        
        List<Creature> monsters = Room.FindBuffTargets(this, 
            new List<GameObjectType> { GameObjectType.Monster }, SkillRange).Cast<Creature>().ToList();
        
        if (!monsters.Any()) return;
        foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())
            BuffManager.Instance.AddBuff(BuffId.MoveSpeedIncrease, monster, MoveSpeedParam);
        foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())
            BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, monster, AttackSpeedParam);

        if (_attackBuff)
        {
            foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())
                BuffManager.Instance.AddBuff(BuffId.AttackIncrease, monster, AttackBuffParam);
        }

        if (_defenceBuff)
        {
            foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())
                BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, monster, DefenceBuffParam);
        }
    }
}
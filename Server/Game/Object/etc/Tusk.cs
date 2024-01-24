using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.etc;

public class Tusk : Monster
{
    public override void Init()
    {
        DataManager.MonsterDict.TryGetValue(MonsterNum, out var monsterData);
        Stat.MergeFrom(monsterData?.stat);
        if (Room == null) return;
        Time = Room.Stopwatch.ElapsedMilliseconds;
        Hp = MaxHp;
    }
    
    public override void OnDead(GameObject attacker)
    {
        if (Room == null) return;
        Targetable = false;
        if (attacker.Target != null)
        {
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
            {
                if (attacker.Parent != null) 
                    attacker.Parent.Target = null;
            }
            attacker.Target = null;
        }
        
        var diePacket = new S_Die { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);

        var room = Room;
        room.LeaveGame(Id);
    }
    
    protected override void UpdateIdle() { }
    protected override void UpdateMoving() { }
    protected override void UpdateAttack() { }
    public override void OnSkillUpgrade(Skill skill) { }
    public override void SkillInit() { }
}
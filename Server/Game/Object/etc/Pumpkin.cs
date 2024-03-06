using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Object.etc;

public class Pumpkin : Tower
{
    public UnitId OriginalTowerId { get; set; }
    
    public override void Init()
    {
        DataManager.UnitDict.TryGetValue((int)UnitId, out var towerData);
        Stat.MergeFrom(towerData?.stat);
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
        
        S_Die diePacket = new S_Die { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);

        GameRoom room = Room;
        room.LeaveGame(Id);
    }
    
    protected override void UpdateIdle() { }
    protected override void UpdateMoving() { }
    protected override void UpdateAttack() { }
    public override void OnSkillUpgrade(Skill skill) { }
    public override void SkillInit() { }
}
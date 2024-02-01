using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Fence : GameObject
{
    public int FenceNum { get; set; }

    public Fence()
    {
        ObjectType = GameObjectType.Fence;
    }

    public override void Init()
    {
        DataManager.FenceDict.TryGetValue(FenceNum, out var fenceData);
        if (fenceData == null) throw new InvalidDataException();
        Stat.MergeFrom(fenceData.stat);
        Stat.Hp = fenceData.stat.MaxHp;
    }

    public override void OnDead(GameObject attacker)
    {
        if (Room == null) return;
        Targetable = false;

        if (Way == SpawnWay.North) Room.GameInfo.NorthFenceCnt--;
        else Room.GameInfo.SouthFenceCnt--;
        
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
}
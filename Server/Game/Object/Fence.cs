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

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        base.OnDamaged(attacker, damage, damageType, reflected);
        
        if (Room == null) return;
        Room.GameInfo.FenceDamageThisRound += damage;
    }
    
    protected override void OnDead(GameObject? attacker)
    {
        if (Room == null) return;

        Targetable = false;

        if (attacker != null)
        {
            attacker.KillLog = Id;
            attacker.Target = null;
            
            var monster = attacker as Monster ?? attacker.Parent as Monster;
            if (monster != null)
            {
                Room.YieldDna(this, attacker);
            }

            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile && attacker.Parent != null)
            {
                attacker.Parent.Target = null;
            }
        }
        
        Room.Broadcast(new S_Die { ObjectId = Id });
        Room.GameInfo.TheNumberOfDestroyedFence++;
        Room.DieAndLeave(Id);
    }
}
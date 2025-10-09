using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage5001 : Stage
{
    private readonly Dictionary<string, Tower> _towers = new();
    private bool _finishMove = false;
    
    public override void Spawn(int round)
    {
        var npc = Room?.FindPlayer(go => go is Player { IsNpc: true });
        if (Room == null || npc == null) return;
        if (Room.GameInfo.FenceStartPos.Z >= 10 && _finishMove == false)
        {
            FinishMove();
            _finishMove = true;
        }
        
        switch (round)
        {
            case 0:
                var tower0 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(-3, 6, 1));
                var tower1 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(-1.5f, 6, 1));
                var tower2 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(0, 6, 1));
                var tower3 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(1.5f, 6, 1));
                var tower4 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(3, 6, 1));
                _towers.Add("b0", tower0);
                _towers.Add("b1", tower1);
                _towers.Add("b2", tower2);
                _towers.Add("b3", tower3);
                _towers.Add("b4", tower4);
                break;
            case 1:
                Room.UpgradeSkill(Skill.BunnyHealth);
                Room.UpgradeSkill(Skill.BunnyEvasion);
                break;
            case 2:
                Room.UpgradeUnitSingle(_towers, "b1", npc);
                Room.UpgradeUnitSingle(_towers, "b3", npc);
                break;
            case 3:
                Room.UpgradeUnitSingle(_towers, "b0", npc);
                Room.UpgradeUnitSingle(_towers, "b2", npc);
                Room.UpgradeUnitSingle(_towers, "b4", npc);
                break;
            case 5:
                var tower5 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(-4.5f, 6, 1));
                var tower6 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(4.5f, 6, 1));
                _towers.Add("b5", tower5);
                _towers.Add("b6", tower6);
                break;
            case 6:
                Room.UpgradeUnitSingle(_towers, "b5", npc);
                Room.UpgradeUnitSingle(_towers, "b6", npc);
                break;
            case 7:
                Room.UpgradeSkill(Skill.RabbitAggro);
                break;
            case 8:
                Room.UpgradeSkill(Skill.RabbitDefence);
                break;
            case 9:
                Room.UpgradeSkill(Skill.RabbitEvasion);
                break;
            case 12:
                Room.UpgradeUnitSingle(_towers, "b0", npc);
                Room.UpgradeUnitSingle(_towers, "b2", npc);
                Room.UpgradeUnitSingle(_towers, "b4", npc);
                Room.UpgradeUnitSingle(_towers, "b6", npc);
                break;
            case 13:
                Room.UpgradeUnitSingle(_towers, "b1", npc);
                Room.UpgradeUnitSingle(_towers, "b3", npc);
                Room.UpgradeUnitSingle(_towers, "b5", npc);
                break;
        }
    }
    
    private void FinishMove()
    {
        if (Room == null) return;
        Room.SpawnTowerOnRelativeZ(UnitId.Hare, new Vector3(-1, 6, 5));
        Room.SpawnTowerOnRelativeZ(UnitId.Hare, new Vector3(1, 6, 5));
    }
}
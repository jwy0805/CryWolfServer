using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage5001 : Stage
{
    private readonly Dictionary<int, Tower> _towers = new();

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;
        
        switch (round)
        {
            case 0:
                var tower0 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(-3, 0, 1));
                var tower1 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(-1.5f, 0, 1));
                var tower2 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(0, 0, 1));
                var tower3 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(1.5f, 0, 1));
                var tower4 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(3, 0, 1));
                _towers.Add(0, tower0);
                _towers.Add(1, tower1);
                _towers.Add(2, tower2);
                _towers.Add(3, tower3);
                _towers.Add(4, tower4);
                break;
            case 1:
                Room.UpgradeSkill(Skill.BunnyHealth);
                Room.UpgradeSkill(Skill.BunnyEvasion);
                break;
            case 2:
                Room.UpgradeUnit(_towers[1], npc);
                Room.UpgradeUnit(_towers[3], npc);
                break;
            case 3:
                Room.UpgradeUnit(_towers[0], npc);
                Room.UpgradeUnit(_towers[2], npc);
                Room.UpgradeUnit(_towers[4], npc);
                break;
            case 5:
                var tower5 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(-4.5f, 0, 1));
                var tower6 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(4.5f, 0, 1));
                _towers.Add(5, tower5);
                _towers.Add(6, tower6);
                break;
            case 6:
                Room.UpgradeUnit(_towers[5], npc);
                Room.UpgradeUnit(_towers[6], npc);
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
                Room.UpgradeUnit(_towers[0], npc);
                Room.UpgradeUnit(_towers[2], npc);
                Room.UpgradeUnit(_towers[4], npc);
                Room.UpgradeUnit(_towers[6], npc);
                break;
            case 13:
                Room.UpgradeUnit(_towers[1], npc);
                Room.UpgradeUnit(_towers[3], npc);
                Room.UpgradeUnit(_towers[5], npc);
                break;
        }
    }
}
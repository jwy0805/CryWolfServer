using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage5002 : Stage
{
    private readonly Dictionary<float, Tower> _towers = new();

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;

        switch (round)
        {
            case 0:
                var tower0 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(-3.5f, 0, 1));
                var tower1 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(-1, 0, 1));
                var tower3 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(3.5f, 0, 1)); 
                var tower4 = Room.SpawnTowerOnRelativeZ(UnitId.Mushroom, new Vector3(-2, 0, -1));
                var tower5 = Room.SpawnTowerOnRelativeZ(UnitId.Mushroom, new Vector3(2, 0, -1));
                _towers.Add(0, tower0);
                _towers.Add(1, tower1);
                _towers.Add(3, tower3);
                _towers.Add(4, tower4);
                _towers.Add(5, tower5);
                break;
            case 1:
                var tower2 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(1, 0, 1));
                var tower6 = Room.SpawnTowerOnRelativeZ(UnitId.Mushroom, new Vector3(-1, 0, -1));
                _towers.Add(2, tower2);
                _towers.Add(6, tower6);
                
                Room.UpgradeBaseSkill(Skill.AssetSheep, npc);
                break;
            case 2:
                Room.UpgradeSkill(Skill.BunnyHealth);
                Room.UpgradeSkill(Skill.BunnyEvasion);
                break;
            case 3:
                
                break;
        }
    }
}
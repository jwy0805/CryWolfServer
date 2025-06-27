using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage5002 : Stage
{
    private readonly Dictionary<string, Tower> _towers = new();
    private bool _finishMove = false;

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;
        if (Room.GameInfo.FenceStartPos.Z >= 10 && _finishMove == false)
        {
            FinishMove();
            _finishMove = true;
        }

        Random random = new();
        if (random.Next(0, 100) < 30)
        {
            Room.UpgradeBaseSkill(Skill.RepairSheep, npc);
        }
        
        switch (round)
        {
            case 0:
                var tower0 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(-3.5f, 6, 1));
                var tower1 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(-1, 6, 1));
                var tower3 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(3.5f, 6, 1)); 
                var tower4 = Room.SpawnTowerOnRelativeZ(UnitId.Mushroom, new Vector3(-2, 6, -1));
                var tower5 = Room.SpawnTowerOnRelativeZ(UnitId.Mushroom, new Vector3(2, 6, -1));
                _towers.Add("p1", tower0);
                _towers.Add("p4", tower1);
                _towers.Add("p3", tower3);
                _towers.Add("m1", tower4);
                _towers.Add("m2", tower5);
                break;
            case 1:
                var tower2 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(1, 6, 1));
                var tower6 = Room.SpawnTowerOnRelativeZ(UnitId.Mushroom, new Vector3(0, 6, -1));
                _towers.Add("p2", tower2);
                _towers.Add("m3", tower6);
                Room.UpgradeBaseSkill(Skill.AssetSheep, npc);
                break;
            case 2:
                Room.UpgradeSkill(Skill.PracticeDummyHealth);
                Room.UpgradeSkill(Skill.PracticeDummyHealth2);
                break;
            case 3:
                Room.UpgradeSkill(Skill.MushroomAttack);
                Room.UpgradeSkill(Skill.MushroomRange);
                Room.UpgradeSkill(Skill.MushroomClosestAttack);
                break;
            case 4:
                Room.UpgradeUnit(_towers["p1"], npc);
                Room.UpgradeUnit(_towers["p3"], npc);
                break;
            case 5:
                Room.UpgradeUnit(_towers["m2"], npc);
                Room.UpgradeBaseSkill(Skill.AssetSheep, npc);
                break;
            case 6:
                Room.UpgradeUnit(_towers["m1"], npc);
                Room.UpgradeUnit(_towers["m3"], npc);
                break;
            case 8:
                Room.UpgradeSkill(Skill.FungiPoison);
                Room.UpgradeSkill(Skill.FungiPoisonResist);
                Room.UpgradeSkill(Skill.FungiClosestHeal);
                break;
            case 9:
                Room.UpgradeUnit(_towers["p2"], npc);
                Room.UpgradeUnit(_towers["p4"], npc);
                break;
            case 10:
                Room.UpgradeSkill(Skill.TrainingDummyFaintAttack);
                Room.UpgradeSkill(Skill.TrainingDummyAccuracy);
                Room.UpgradeSkill(Skill.TrainingDummyHealth);
                break;
            case 11:
                var tower7 = Room.SpawnTowerOnRelativeZ(UnitId.Fungi, new Vector3(0, 6, -6));
                _towers.Add("m4", tower7);
                break;
        }
    }
    
    private void FinishMove()
    {
        if (Room == null) return;
        Room.SpawnTowerOnRelativeZ(UnitId.Toadstool, new Vector3(0, 6, 2));
        Room.SpawnTowerOnRelativeZ(UnitId.Toadstool, new Vector3(-1.5f, 6, 2));
    }
}
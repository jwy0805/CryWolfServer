using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage5005 : Stage
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
        if (random.Next(0, 100) < 60)
        {
            Room.UpgradeBaseSkill(Skill.RepairSheep, npc);
        }

        switch (round)
        {
            case 0:
                var tower1 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(-2.5f, 6, 1));
                var tower2 = Room.SpawnTowerOnRelativeZ(UnitId.Shell, new Vector3(0, 6, 1));
                var tower3 = Room.SpawnTowerOnRelativeZ(UnitId.Bunny, new Vector3(2.5f, 6, 1)); 
                var tower4 = Room.SpawnTowerOnRelativeZ(UnitId.Mushroom, new Vector3(1.5f, 6, -1));
                var tower5 = Room.SpawnTowerOnRelativeZ(UnitId.Mushroom, new Vector3(-1.5f, 6, -1));
                _towers.Add("r1", tower1);
                _towers.Add("s1", tower2);
                _towers.Add("r2", tower3);
                _towers.Add("m1", tower4);
                _towers.Add("m2", tower5);
                break;
            case 1:
                Room.UpgradeBaseSkill(Skill.AssetSheep, npc);
                var tower6 = Room.SpawnTowerOnRelativeZ(UnitId.Seed, new Vector3(4.5f, 6, -1));
                var tower7 = Room.SpawnTowerOnRelativeZ(UnitId.Seed, new Vector3(-4.5f, 6, -1));
                _towers.Add("f1", tower6);
                _towers.Add("f2", tower7);
                break;
            case 2:
                Room.UpgradeSkill(Skill.ShellDefence);
                Room.UpgradeSkill(Skill.ShellFireResist);
                Room.UpgradeSkill(Skill.ShellPoisonResist);
                break;
            case 3:
                Room.UpgradeUnitSingle(_towers, "r1", npc);
                Room.UpgradeUnitSingle(_towers, "r2", npc);
                break;
            case 4:
                Room.UpgradeSkill(Skill.SeedEvasion);
                Room.UpgradeSkill(Skill.SeedRange);
                break;
            case 5:
                Room.UpgradeSkill(Skill.MushroomAttack);
                Room.UpgradeSkill(Skill.MushroomRange);
                Room.UpgradeSkill(Skill.MushroomClosestAttack);
                Room.UpgradeUnitSingle(_towers, "s1", npc);
                break;
            case 6:
                Room.UpgradeUnitSingle(_towers, "m1", npc);
                Room.UpgradeUnitSingle(_towers, "m2", npc);
                Room.UpgradeBaseSkill(Skill.AssetSheep, npc);
                break;
            case 7:
                Room.UpgradeBaseSkill(Skill.BaseUpgradeSheep, npc);
                Room.UpgradeUnitSingle(_towers, "f1", npc);
                Room.UpgradeUnitSingle(_towers, "f2", npc);
                break;
            case 8:
                var tower8 = Room.SpawnTowerOnRelativeZ(UnitId.Spike, new Vector3(-4.5f, 6, 1));
                var tower9 = Room.SpawnTowerOnRelativeZ(UnitId.Spike, new Vector3(4.5f, 6, 1));
                _towers.Add("s2", tower8);
                _towers.Add("s3", tower9);
                Room.UpgradeSkill(Skill.FungiPoison);
                Room.UpgradeSkill(Skill.FungiPoisonResist);
                Room.UpgradeSkill(Skill.FungiClosestHeal);
                break;
            case 9:
                Room.UpgradeBaseSkill(Skill.AssetSheep, npc);
                var tower10 = Room.SpawnTowerOnRelativeZ(UnitId.Fungi, new Vector3(0, 6, -3.5f));
                _towers.Add("m3", tower10);
                break;
            case 10:
                Room.UpgradeUnitSingle(_towers, "r1", npc);
                Room.UpgradeUnitSingle(_towers, "r2", npc);
                Room.UpgradeSkill(Skill.HarePunch);
                break;
            case 11:
                Room.UpgradeUnitSingle(_towers, "m1", npc);
                Room.UpgradeUnitSingle(_towers, "m2", npc);
                Room.UpgradeUnitSingle(_towers, "m3", npc);
                Room.UpgradeSkill(Skill.ToadstoolClosestAttackAll);
                Room.UpgradeSkill(Skill.ToadstoolPoisonResist);
                Room.UpgradeSkill(Skill.ToadstoolNestedPoison);
                Room.UpgradeSkill(Skill.ToadstoolPoisonCloud);
                break;
            case 12:
                Room.UpgradeUnitSingle(_towers, "s1", npc);
                Room.UpgradeUnitSingle(_towers, "s2", npc);
                Room.UpgradeUnitSingle(_towers, "s3", npc);
                break;
            case 13:
                Room.UpgradeBaseSkill(Skill.BaseUpgradeSheep, npc);
                Room.UpgradeSkill(Skill.HermitNormalAttackDefence);
                Room.UpgradeSkill(Skill.HermitAttackerFaint);
                Room.UpgradeSkill(Skill.HermitRecoverBurn);
                break;
            case 14:
                Room.UpgradeUnitSingle(_towers, "f1", npc);
                Room.UpgradeUnitSingle(_towers, "f2", npc);
                break;
            case 15:
                Room.UpgradeSkill(Skill.FlowerPot3Hit);
                Room.UpgradeSkill(Skill.FlowerPotFireResistDown);
                Room.UpgradeSkill(Skill.FlowerPotDoubleTargets);
                break;
        }
    }

    private void FinishMove()
    {
        Room?.SpawnTowerOnRelativeZ(UnitId.FlowerPot, new Vector3(0, 6, 2.5f));
    }
}
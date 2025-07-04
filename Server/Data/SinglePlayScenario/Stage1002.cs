using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage1002 : Stage
{
    private readonly Dictionary<string, MonsterStatue> _statues = new();

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;

        switch (round)
        {
            case 0:
                var statue1 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = -1, PosY = 6, PosZ = 13 });
                var statue2 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = 0, PosY = 6, PosZ = 13 });
                var statue3 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = 3, PosY = 6, PosZ = 13 });
                var statue4 = Room.SpawnStatue(UnitId.Snakelet, new PositionInfo { PosX = -1, PosY = 6, PosZ = 14.5f });
                _statues.Add("d1", statue1);
                _statues.Add("d2", statue2);
                _statues.Add("d3", statue3);
                _statues.Add("s4", statue4);
                break;
            case 1:
                var statue5 = Room.SpawnStatue(UnitId.Snakelet, new PositionInfo { PosX = -2, PosY = 6, PosZ = 14.5f });
                var statue6 = Room.SpawnStatue(UnitId.Snakelet, new PositionInfo { PosX = 2, PosY = 6, PosZ = 14.5f });
                _statues.Add("s5", statue5);
                _statues.Add("s6", statue6);
                break;
            case 2:
                Room.UpgradeSkill(Skill.DogPupSpeed);
                Room.UpgradeSkill(Skill.DogPupEvasion);
                Room.UpgradeSkill(Skill.DogPupAttackSpeed);
                break;
            case 3:
                Room.UpgradeSkill(Skill.SnakeletAttackSpeed);
                Room.UpgradeSkill(Skill.SnakeletAttack);
                break;
            case 4:
                Room.UpgradeSkill(Skill.SnakeletEvasion);
                Room.UpgradeStatue(_statues,"d1", npc);
                break;
            case 5:
                Room.UpgradeStatue(_statues,"d2", npc);
                Room.UpgradeStatue(_statues,"d3", npc);
                break;
            case 6:
                Room.UpgradeSkill(Skill.DogBarkAdjacentAttackSpeed);
                Room.UpgradeSkill(Skill.DogBarkFireResist);
                Room.UpgradeSkill(Skill.DogBarkFourthAttack);
                break;
            case 7:
                Room.UpgradeBaseSkill(Skill.AssetWolf, npc);
                Room.UpgradeStatue(_statues,"s4", npc);
                break;
            case 8:
                Room.UpgradeSkill(Skill.SnakeFire);
                Room.UpgradeSkill(Skill.SnakeAccuracy);
                break;
            case 9:
                Room.UpgradeSkill(Skill.SnakeFireResist);
                Room.UpgradeSkill(Skill.SnakeSpeed);
                break;
            case 10:
                Room.UpgradeStatue(_statues,"s5", npc);
                Room.UpgradeStatue(_statues,"s6", npc);
                break;
            case 11:
                Room.UpgradeStatue(_statues,"d1", npc);
                Room.UpgradeStatue(_statues,"d2", npc);
                Room.UpgradeStatue(_statues,"d3", npc);
                break;
            case 12:
                Room.UpgradeSkill(Skill.DogBowwowSmash);
                Room.UpgradeSkill(Skill.DogBowwowSmashFaint);
                Room.UpgradeStatue(_statues,"s5", npc);
                break;
            case 13:
                Room.UpgradeBaseSkill(Skill.AssetWolf, npc);
                break;
            case 14:
                Room.UpgradeStatue(_statues,"s4", npc);
                Room.UpgradeStatue(_statues,"s6", npc);
                break;
            case 15:
                Room.UpgradeSkill(Skill.SnakeAccuracy);
                Room.UpgradeSkill(Skill.SnakeFire);
                Room.UpgradeSkill(Skill.SnakeFireResist);
                Room.UpgradeSkill(Skill.SnakeSpeed);
                break;
        }
    }
}
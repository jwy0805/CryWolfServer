using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage1001 : Stage
{
    private readonly Dictionary<int, MonsterStatue> _statues = new();
    
    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;    
        
        switch (round)
        {
            case 0:
                var statue0 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = -3, PosY = 6, PosZ = 13 });
                var statue1 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = -1.5f, PosY = 6, PosZ = 13 });
                var statue3 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = 1.5f, PosY = 6, PosZ = 13 });
                var statue4 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = 3, PosY = 6, PosZ = 13 });
                _statues.Add(0, statue0);
                _statues.Add(1, statue1);
                _statues.Add(3, statue3);
                _statues.Add(4, statue4);
                break;
            case 1:
                Room.UpgradeSkill(Skill.DogPupSpeed);
                Room.UpgradeSkill(Skill.DogPupEvasion);
                break;
            case 2:
                Room.UpgradeSkill(Skill.DogPupAttackSpeed);
                break;
            case 3:
                Room.UpgradeUnit(_statues[1], npc);
                break;
            case 5:
                Room.UpgradeUnit(_statues[3], npc);
                Room.UpgradeUnit(_statues[0], npc);
                Room.UpgradeUnit(_statues[4], npc);
                break;
            case 6:
                Room.UpgradeSkill(Skill.DogBarkAdjacentAttackSpeed);
                Room.UpgradeSkill(Skill.DogBarkFireResist);
                Room.UpgradeSkill(Skill.DogBarkFourthAttack);
                break;
            case 9:
                var statue5 = Room.SpawnStatue(UnitId.DogBowwow, new PositionInfo { PosX = 0, PosY = 6, PosZ = 12 });
                var statue6 = Room.SpawnStatue(UnitId.DogBowwow, new PositionInfo { PosX = 0, PosY = 6, PosZ = 14 });
                _statues.Add(5, statue5);
                _statues.Add(6, statue6);
                break;
            case 11:
                Room.UpgradeUnit(_statues[1], npc);
                Room.UpgradeUnit(_statues[2], npc);
                Room.UpgradeUnit(_statues[3], npc);
                break;
            case 13:
                Room.UpgradeUnit(_statues[0], npc);
                Room.UpgradeUnit(_statues[4], npc);
                break;
        }
    }
}
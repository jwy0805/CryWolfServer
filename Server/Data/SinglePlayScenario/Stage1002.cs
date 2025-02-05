using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage1002 : Stage
{
    private readonly Dictionary<float, MonsterStatue> _statues = new();

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;

        switch (round)
        {
            case 0:
                var statue0 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = -3, PosY = 6, PosZ = 13 });
                var statue1 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = -1, PosY = 6, PosZ = 13 });
                var statue2 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = 0, PosY = 6, PosZ = 13 });
                var statue3 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = 3, PosY = 6, PosZ = 13 });
                var statue4 = Room.SpawnStatue(UnitId.SnakeNaga, new PositionInfo { PosX = -1, PosY = 6, PosZ = 14.5f });
                _statues.Add(0, statue0);
                _statues.Add(1, statue1);
                _statues.Add(2, statue2);
                _statues.Add(3, statue3);
                _statues.Add(4, statue4);
                break;
            case 1:
                var statue5 = Room.SpawnStatue(UnitId.SnakeNaga, new PositionInfo { PosX = -2, PosY = 6, PosZ = 14.5f });
                var statue6 = Room.SpawnStatue(UnitId.SnakeNaga, new PositionInfo { PosX = 2, PosY = 6, PosZ = 14.5f });
                _statues.Add(5, statue5);
                _statues.Add(6, statue6);
                break;
            case 2:
                Room.UpgradeSkill(Skill.DogPupSpeed);
                Room.UpgradeSkill(Skill.DogPupEvasion);
                Room.UpgradeSkill(Skill.DogPupAttackSpeed);
                break;
            case 3:
                
                break;
        }
    }
}
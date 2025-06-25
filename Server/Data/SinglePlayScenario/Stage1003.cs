using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage1003 : Stage
{
    private readonly Dictionary<int, MonsterStatue> _statues = new();

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;

        switch (round)
        {
            case 0:
                var statue1 = Room.SpawnStatue(UnitId.Cacti, new PositionInfo { PosX = -2.5f, PosY = 6, PosZ = 12 });
                var statue2 = Room.SpawnStatue(UnitId.Lurker, new PositionInfo { PosX = 0, PosY = 6, PosZ = 12 });
                var statue3 = Room.SpawnStatue(UnitId.Cacti, new PositionInfo { PosX = 2.5f, PosY = 6, PosZ = 12 });
                var statue4 = Room.SpawnStatue(UnitId.Bomb, new PositionInfo { PosX = 1.5f, PosY = 6, PosZ = 14f });
                _statues.Add(1, statue1);
                _statues.Add(2, statue2);
                _statues.Add(3, statue3);
                _statues.Add(4, statue4);
                break;
            case 1:
                Room.UpgradeSkill(Skill.LurkerSpeed);
                Room.UpgradeSkill(Skill.LurkerDefence);
                Room.UpgradeSkill(Skill.LurkerPoisonResist);
                break;
            case 2:
                var statue5 = Room.SpawnStatue(UnitId.Bomb, new PositionInfo { PosX = -1.5f, PosY = 6, PosZ = 14f });
                _statues.Add(5, statue5);
                Room.UpgradeSkill(Skill.BombHealth);
                Room.UpgradeSkill(Skill.BombAttack);
                break;
            case 3:
                Room.UpgradeSkill(Skill.BombBomb);
                Room.UpgradeUnit(_statues[2], npc);
                break;
            case 4:
                Room.UpgradeUnit(_statues[4], npc);
                Room.UpgradeBaseSkill(Skill.AssetWolf, npc);
                break;
            case 5:
                Room.UpgradeUnit(_statues[5], npc);
                break;
            case 6:
                Room.UpgradeSkill(Skill.CactiDefence);
                Room.UpgradeSkill(Skill.CactiDefence2);
                break;
            case 7:
                Room.UpgradeUnit(_statues[1], npc);
                Room.UpgradeUnit(_statues[3], npc);
                break;
            case 8:
                var statue7 = Room.SpawnStatue(UnitId.Creeper, new PositionInfo { PosX = 1.5f, PosY = 6, PosZ = 12 });
                _statues.Add(7, statue7);
                Room.UpgradeSkill(Skill.CactusSpeed);
                Room.UpgradeSkill(Skill.CactusPoisonResist);
                Room.UpgradeSkill(Skill.CreeperPoison);
                Room.UpgradeSkill(Skill.CreeperRoll);
                break;
            case 9:
                Room.UpgradeBaseSkill(Skill.BaseUpgradeWolf, npc);
                Room.UpgradeSkill(Skill.AssetWolf);
                break;
            case 10:
                var statue8 = Room.SpawnStatue(UnitId.SnowBomb, new PositionInfo { PosX = -1.5f, PosY = 6, PosZ = 14 });
                _statues.Add(8, statue8);
                Room.UpgradeSkill(Skill.CactusReflection);
                Room.UpgradeSkill(Skill.CactusReflectionFaint);
                break;
            case 11:
                Room.UpgradeSkill(Skill.CreeperNestedPoison);
                Room.UpgradeSkill(Skill.CreeperRollDamageUp);
                var statue6 = Room.SpawnStatue(UnitId.Cactus, new PositionInfo { PosX = 0f, PosY = 6, PosZ = 14 });
                break;
            case 12:
                Room.UpgradeSkill(Skill.SnowBombFireResist);
                Room.UpgradeSkill(Skill.SnowBombAreaAttack);
                Room.UpgradeSkill(Skill.SnowBombFrostbite);
                Room.UpgradeSkill(Skill.SnowBombFrostArmor);
                break;
            case 13:
                Room.UpgradeUnit(_statues[1], npc);
                Room.UpgradeUnit(_statues[3], npc);
                break;
            case 14:
                Room.UpgradeSkill(Skill.CactusBossRush);
                break;
            case 15:
                Room.UpgradeSkill(Skill.CactusBossBreath);
                break;
        }
    }
}
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage1004 : Stage
{
    private readonly Dictionary<string, MonsterStatue> _statues = new();

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;

        switch (round)
        {
            case 0:
                var statue1 = Room.SpawnStatue(UnitId.WolfPup, new PositionInfo { PosX = 2.5f, PosY = 6, PosZ = 12 });
                var statue2 = Room.SpawnStatue(UnitId.WolfPup, new PositionInfo { PosX = 0, PosY = 6, PosZ = 12 });
                var statue3 = Room.SpawnStatue(UnitId.WolfPup, new PositionInfo { PosX = -2.5f, PosY = 6, PosZ = 12 });
                _statues.Add("w1", statue1);
                _statues.Add("w2", statue2);
                _statues.Add("w3", statue3);
                Room.UpgradeSkill(Skill.WolfPupSpeed);
                Room.UpgradeSkill(Skill.WolfPupAttack);
                break;
            case 1:
                Room.UpgradeSkill(Skill.WolfPupDefence);
                break;
            case 2:
                Room.UpgradeUnit(_statues["w2"], npc);
                break;
            case 3:
                Room.UpgradeUnit(_statues["w1"], npc);
                Room.UpgradeUnit(_statues["w3"], npc);
                break;
            case 5:
                var statue4 = Room.SpawnStatue(UnitId.MoleRat, new PositionInfo { PosX = 4f, PosY = 6, PosZ = 14 });
                var statue5 = Room.SpawnStatue(UnitId.MoleRat, new PositionInfo { PosX = -4f, PosY = 6, PosZ = 14 });
                _statues.Add("mol1", statue4);
                _statues.Add("mol2", statue5);
                break;
            case 6:
                Room.UpgradeSkill(Skill.MoleRatBurrowSpeed);
                Room.UpgradeSkill(Skill.MoleRatBurrowEvasion);
                break;
            case 7:
                Room.UpgradeSkill(Skill.MoleRatDrain);
                break;
            case 8:
                var statue6 = Room.SpawnStatue(UnitId.MosquitoPester, new PositionInfo { PosX = 2, PosY = 6, PosZ = 14 });
                var statue7 = Room.SpawnStatue(UnitId.MosquitoPester, new PositionInfo { PosX = -2, PosY = 6, PosZ = 14 });
                _statues.Add("mos1", statue6);
                _statues.Add("mos2", statue7);
                break;
            case 10:
                Room.UpgradeSkill(Skill.MosquitoPesterPoison);
                Room.UpgradeSkill(Skill.MosquitoPesterWoolRate);
                Room.UpgradeSkill(Skill.MosquitoPesterPoisonResist);
                Room.UpgradeSkill(Skill.MosquitoPesterEvasion);
                Room.UpgradeSkill(Skill.MosquitoPesterWoolRate);
                break;
            case 11:
                var statue8 = Room.SpawnStatue(UnitId.MoleRatKing, new PositionInfo { PosX = 5, PosY = 6, PosZ = 12 });
                _statues.Add("mol3", statue8);
                Room.UpgradeSkill(Skill.MoleRatKingBurrow);
                break;
            case 12:
                Room.UpgradeSkill(Skill.WolfHealth);
                Room.UpgradeSkill(Skill.WolfMagicalAttack);
                Room.UpgradeSkill(Skill.WolfDrain);
                Room.UpgradeSkill(Skill.WolfCritical);
                break;
        }
    }
}
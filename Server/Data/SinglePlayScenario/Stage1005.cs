using Google.Protobuf.Protocol;
using Newtonsoft.Json.Serialization;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage1005 : Stage
{
    private readonly Dictionary<string, MonsterStatue> _statues = new();

    public override void Spawn(int round)
    {
        var npc = Room?.FindPlayer(go => go is Player { IsNpc: true });
        if (Room == null || npc == null) return;

        switch (round)
        {
            case 0:
                var statue1 = Room.SpawnStatue(UnitId.Cacti, new PositionInfo { PosX = -2.5f, PosY = 6, PosZ = 12 });
                var statue2 = Room.SpawnStatue(UnitId.Cacti, new PositionInfo { PosX = 2.5f, PosY = 6, PosZ = 12 });
                var statue3 = Room.SpawnStatue(UnitId.Snakelet, new PositionInfo { PosX = 0, PosY = 6, PosZ = 14 });
                var statue4 = Room.SpawnStatue(UnitId.Lurker, new PositionInfo { PosX = 3.5f, PosY = 6, PosZ = 14 });
                var statue5 = Room.SpawnStatue(UnitId.Lurker, new PositionInfo { PosX = -3.5f, PosY = 6, PosZ = 14 });
                _statues.Add("c1", statue1);
                _statues.Add("c2", statue2);
                _statues.Add("s1", statue3);
                _statues.Add("l1", statue4);
                _statues.Add("l2", statue5);
                break;
            case 2:
                var statue6 = Room.SpawnStatue(UnitId.WolfPup, new PositionInfo { PosX = -4f, PosY = 6, PosZ = 12 });
                var statue7 = Room.SpawnStatue(UnitId.WolfPup, new PositionInfo { PosX = 4f, PosY = 6, PosZ = 12 });
                _statues.Add("w1", statue6);
                _statues.Add("w2", statue7);
                break;
            case 3:
                Room.UpgradeSkill(Skill.CactiDefence);
                Room.UpgradeSkill(Skill.CactiDefence2);
                break;
            case 4:
                Room.UpgradeStatue(_statues,"s1", npc);
                break;
            case 5:
                Room.UpgradeStatue(_statues,"c1", npc);
                Room.UpgradeStatue(_statues,"c2", npc);
                break;
            case 6:
                Room.UpgradeBaseSkill(Skill.BaseUpgradeWolf, npc);
                break;
            case 7:
                Room.UpgradeBaseSkill(Skill.AssetWolf, npc);
                Room.UpgradeSkill(Skill.WolfPupSpeed);
                Room.UpgradeSkill(Skill.WolfPupAttack);
                Room.UpgradeSkill(Skill.WolfPupDefence);
                break;
            case 8:
                Room.UpgradeBaseSkill(Skill.AssetWolf, npc);
                Room.UpgradeStatue(_statues,"w1", npc);
                Room.UpgradeStatue(_statues,"w2", npc);
                Room.UpgradeStatue(_statues,"l1", npc);
                Room.UpgradeStatue(_statues,"l2", npc);
                break;
            case 9:
                Room.UpgradeBaseSkill(Skill.BaseUpgradeWolf, npc);
                break;
            case 10:
                Room.UpgradeSkill(Skill.SnakeFire);
                Room.UpgradeSkill(Skill.SnakeAccuracy);
                Room.UpgradeSkill(Skill.WolfHealth);
                Room.UpgradeSkill(Skill.WolfMagicalAttack);
                Room.UpgradeSkill(Skill.WolfDrain);
                Room.UpgradeSkill(Skill.WolfCritical);
                Room.UpgradeSkill(Skill.WolfLastHitDna);
                break;
            case 11:
                Room.UpgradeStatue(_statues,"w1", npc);
                Room.UpgradeStatue(_statues,"l1", npc);
                break;
            case 12:
                Room.UpgradeStatue(_statues,"w2", npc);
                Room.UpgradeStatue(_statues,"l2", npc);
                Room.UpgradeSkill(Skill.WerewolfThunder);
                break;
            case 13:
                Room.UpgradeSkill(Skill.HorrorPoisonImmunity);
                Room.UpgradeSkill(Skill.HorrorRollPoison);
                Room.UpgradeSkill(Skill.HorrorPoisonSmog);
                break;
        }
    }
}
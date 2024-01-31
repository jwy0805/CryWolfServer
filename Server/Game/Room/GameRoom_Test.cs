using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    private void T_RegisterMonsterStatue(MonsterStatue statue)
    {
        MonsterSlot monsterSlot = new(statue.MonsterId, statue.Way, statue);

        if (monsterSlot.Way == SpawnWay.North)
        {
            _northMonsters.Add(monsterSlot);
        }
        else
        {
            _southMonsters.Add(monsterSlot);
        }
    }
    
    private void T_SpawnMonstersInNewRound()
    {
        var slots = _northMonsters.Concat(_southMonsters).ToList();
        
        foreach (var slot in slots)
        {
            var player = _npc;
            var monster = EnterMonster((int)slot.MonsterId, FindMonsterSpawnPos(slot.Statue), player);
            monster.StatueId = slot.Statue.Id;
            EnterGame(monster);
            
            HandleSkillInit(player, new C_SkillInit { ObjectId = monster.Id });
        }
    }
    
    private void T_HandleSkillUpgrade(Player? player, C_SkillUpgrade upgradePacket)
    {
        if (player == null) return;
        
        var skill = upgradePacket.Skill;
        if (Enum.IsDefined(typeof(Skill), skill.ToString())) 
            player.SkillSubject.SkillUpgraded(skill);
        else ProcessingBaseSkill(player);
    }
    
    private void SetTutorialRound(int round)
    {
        var player = _npc;
        switch (round)
        {
            case 0:
                // PositionInfo pos1 = new() { PosX = -2, PosY = 6, PosZ = 20, Dir = 0, State = State.Idle };
                // PositionInfo pos2 = new() { PosX = 2, PosY = 6, PosZ = 20, Dir = 0, State = State.Idle };
                // var wolfPupStatue1 = EnterMonsterStatue(1, pos1, _npc);
                // var wolfPupStatue2 = EnterMonsterStatue(1, pos2, _npc);
                // RegisterMonsterStatue(wolfPupStatue1);
                // RegisterMonsterStatue(wolfPupStatue2);
                // EnterGame(wolfPupStatue1);
                // EnterGame(wolfPupStatue2);
                
                PositionInfo pos1 = new() { PosX = -2, PosY = 6, PosZ = 20, Dir = 0, State = State.Idle };
                var wolfPupStatue1 = EnterMonsterStatue((int)MonsterId.Werewolf, pos1, player);
                T_RegisterMonsterStatue(wolfPupStatue1);
                EnterGame(wolfPupStatue1);
                
                player.SkillSubject.SkillUpgraded(Skill.WerewolfThunder);
                player.SkillUpgradedList.Add(Skill.WerewolfThunder);
                break;
            
            case 1:
                PositionInfo pos3 = new() { PosX = 0, PosY = 6, PosZ = 18, Dir = 0, State = State.Idle };
                PositionInfo pos4 = new() { PosX = 0, PosY = 6, PosZ = 21, Dir = 0, State = State.Idle };
                var lurkerStatue1 = EnterMonsterStatue(4, pos3, player);
                var snakeletStatue1 = EnterMonsterStatue(7, pos4, player);
                RegisterMonsterStatue(lurkerStatue1);
                RegisterMonsterStatue(snakeletStatue1);
                EnterGame(lurkerStatue1);
                EnterGame(snakeletStatue1);
                break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                break;
            case 6:
                break;
            case 7:
                break;
            case 8:
                break;
            case 9:
                break;
            case 10:
                break;
            case 11:
                break;
            default: return;
        }
    }
}
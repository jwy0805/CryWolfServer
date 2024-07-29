using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    // For single game logic
    private void RenewTowerSlot(TowerSlot slot, Tower tower)
    {
        slot.TowerId = tower.UnitId;
        slot.PosInfo = tower.PosInfo;
        slot.Way = tower.Way;
        slot.ObjectId = tower.Id;
    }
    
    private void T_RegisterMonsterStatue(MonsterStatue statue)
    {
        MonsterSlot monsterSlot = new(statue.UnitId, statue.Way, statue);

        if (monsterSlot.Way == SpawnWay.North)
        {
            _northMonsters.Add(monsterSlot);
        }
        else
        {
            _southMonsters.Add(monsterSlot);
        }
    }
    
    private void RenewMonsterStatue(MonsterStatue oldStatue, MonsterStatue newStatue)
    {
        if (oldStatue.Way == SpawnWay.North)
        {
            var oldSlot = _northMonsters.FirstOrDefault(slot => slot.Statue.Id == oldStatue.Id);
            int index = _northMonsters.IndexOf(oldSlot);
            if (index != -1)
            {
                _northMonsters[index] = new MonsterSlot(newStatue.UnitId, newStatue.Way, newStatue);
            }
        }
        else
        {
            var oldSlot = _southMonsters.FirstOrDefault(slot => slot.Statue.Id == oldStatue.Id);
            int index = _southMonsters.IndexOf(oldSlot);
            if (index != -1)
            {
                _southMonsters[index] = new MonsterSlot(newStatue.UnitId, newStatue.Way, newStatue);
            }
        }
    }
    
    private void SpawnStatue(UnitId monsterId, PositionInfo pos)
    {
        var player = _npc;
        var statue = SpawnMonsterStatue(monsterId, pos, player);
        T_RegisterMonsterStatue(statue);
        SpawnEffect(EffectId.Upgrade, statue);
    }
    
    private void SkillUpgrade(Skill skill)
    {
        var player = _npc;
        player.SkillSubject.SkillUpgraded(skill);
        player.SkillUpgradedList.Add(skill);
    }
    
    private void UnitUpgrade(MonsterStatue statue)
    {
        int oldStatueId = statue.Id;
        PositionInfo newPos = new()
        {
            PosX = statue.PosInfo.PosX, PosY = statue.PosInfo.PosY, PosZ = statue.PosInfo.PosZ,
        };
        var monsterId = statue.UnitId + 1;
        var player = _npc;
        var newStatue = SpawnMonsterStatue(monsterId, newPos, player);
        LeaveGame(oldStatueId);
        RenewMonsterStatue(statue, newStatue);
        SpawnEffect(EffectId.Upgrade, statue);
    }
    
    private void SetTutorialRound(int round)
    {
        List<MonsterStatue> upgradeStatueList = new();
        if (GameInfo.SheepCount == 0) Broadcast(new S_ShowResultPopup { Win = false });
        else if (round == 11) Broadcast(new S_ShowResultPopup { Win = true });

        switch (round)
        {
            case 0: // 북 2
                PositionInfo pos1 = new() { PosX = -5, PosY = 6, PosZ = 12, State = State.Idle };
                PositionInfo pos2 = new() { PosX = 3, PosY = 6, PosZ = 12, State = State.Idle };
                SpawnStatue(UnitId.WolfPup, pos1);
                SpawnStatue(UnitId.WolfPup, pos2);
                break;
            
            case 1: // 북 4
                PositionInfo pos3 = new() { PosX = 0, PosY = 6, PosZ = 13, State = State.Idle };
                PositionInfo pos4 = new() { PosX = -3, PosY = 6, PosZ = 15, State = State.Idle };
                SpawnStatue(UnitId.Lurker, pos3);
                SpawnStatue(UnitId.Snakelet, pos4);
                break;
            
            case 2:
                break;
            
            case 3: // 북 4 남 1
                SkillUpgrade(Skill.LurkerDefence);
                
                PositionInfo pos5 = new() { PosX = 1.5f, PosY = 6, PosZ = 13, State = State.Idle };
                SpawnStatue(UnitId.WolfPup, pos5);
                
                break;
            
            case 4:
                SkillUpgrade(Skill.SnakeletAttack);
                
                var northWolfPup = _statues.Values
                    .FirstOrDefault(statue => statue is { UnitId: UnitId.WolfPup, Way: SpawnWay.North });
                if (northWolfPup != null) UnitUpgrade(northWolfPup);
                break;
            
            case 5: 
                var northLurker = _statues.Values
                    .FirstOrDefault(statue => statue is { UnitId: UnitId.Lurker, Way: SpawnWay.North });
                if (northLurker != null) UnitUpgrade(northLurker);
                break;
            
            case 6:
                SkillUpgrade(Skill.SnakeletAttackSpeed);
                SkillUpgrade(Skill.SnakeAccuracy);
                
                break;
            
            case 7:
                SkillUpgrade(Skill.CreeperRoll);
                
                PositionInfo pos7 = new() { PosX = -3, PosY = 6, PosZ = 12, State = State.Idle };
                SpawnStatue(UnitId.Werewolf, pos7);
                
                var northWolfpup = _statues.Values
                    .FirstOrDefault(statue => statue is { UnitId: UnitId.WolfPup, Way: SpawnWay.North });
                if (northWolfpup != null) UnitUpgrade(northWolfpup);
                break;
            
            case 8:
                
                var northCreeper = _statues.Values
                    .FirstOrDefault(statue => statue is { UnitId: UnitId.Creeper, Way: SpawnWay.North });
                if (northCreeper != null) UnitUpgrade(northCreeper);
                break;
            
            case 9:
                SkillUpgrade(Skill.HorrorRollPoison);
                break;
            case 10:
                break;
            case 11:
                break;
            default: return;
        }
    }
}
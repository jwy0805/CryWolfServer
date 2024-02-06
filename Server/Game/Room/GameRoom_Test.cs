using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    List<MonsterStatue> _testStatues = new();

    private void RenewTowerSlot(TowerSlot slot, Tower tower)
    {
        slot.TowerId = tower.TowerId;
        slot.PosInfo = tower.PosInfo;
        slot.Way = tower.Way;
        slot.ObjectId = tower.Id;
    }
    
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
    
    private void T_RenewMonsterStatue(MonsterStatue oldStatue, MonsterStatue newStatue)
    {
        if (oldStatue.Way == SpawnWay.North)
        {
            var oldSlot = _northMonsters.FirstOrDefault(slot => slot.Statue.Id == oldStatue.Id);
            int index = _northMonsters.IndexOf(oldSlot);
            if (index != -1)
            {
                _northMonsters[index] = new MonsterSlot(newStatue.MonsterId, newStatue.Way, newStatue);
            }
        }
        else
        {
            var oldSlot = _southMonsters.FirstOrDefault(slot => slot.Statue.Id == oldStatue.Id);
            int index = _southMonsters.IndexOf(oldSlot);
            if (index != -1)
            {
                _southMonsters[index] = new MonsterSlot(newStatue.MonsterId, newStatue.Way, newStatue);
            }
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
    
    private void T_SpawnStatue(MonsterId monsterId, PositionInfo pos)
    {
        var player = _npc;
        var statue = EnterMonsterStatue((int)monsterId, pos, player);
        T_RegisterMonsterStatue(statue);
        EnterGame(statue);
        _testStatues.Add(statue);

        EffectSetting(statue);
    }

    private void EffectSetting(GameObject master)
    {
        var effect = ObjectManager.Instance.CreateEffect(EffectId.Upgrade);
        effect.Room = this;
        effect.Target = master;
        effect.PosInfo = master.PosInfo;
        effect.Info.PosInfo = master.Info.PosInfo;
        effect.Init();
        effect.Info.Name = effect.EffectId.ToString();
        EnterGameParent(effect, master);
    }
    
    private void T_SkillUpgrade(Skill skill)
    {
        var player = _npc;
        player.SkillSubject.SkillUpgraded(skill);
        player.SkillUpgradedList.Add(skill);
    }

    private void T_UnitUpgrade(MonsterStatue statue)
    {
        int oldStatueId = statue.Id;
        PositionInfo newPos = new()
        {
            PosX = statue.PosInfo.PosX, PosY = statue.PosInfo.PosY, PosZ = statue.PosInfo.PosZ,
        };
        var monsterId = (int)statue.MonsterId + 1;
        var player = _npc;
        var newStatue = EnterMonsterStatue(monsterId, newPos, player);
        T_RenewMonsterStatue(statue, newStatue);

        _testStatues.Remove(statue);
        LeaveGame(oldStatueId);
        Broadcast(new S_Despawn { ObjectIds = { oldStatueId }});
        EnterGame(newStatue);
        _testStatues.Add(newStatue);
        
        EffectSetting(statue);
    }
    
    private void SetTutorialRound(int round)
    {
        List<MonsterStatue> upgradeStatueList = new();
        if (GameInfo.SheepCount == 0) Broadcast(new S_ShowResultPopup { Win = false });
        else if (round == 11) Broadcast(new S_ShowResultPopup { Win = true });

        switch (round)
        {
            case 0: // 북 2
                PositionInfo pos1 = new() { PosX = -5, PosY = 6, PosZ = 21, Dir = 0, State = State.Idle };
                PositionInfo pos2 = new() { PosX = 3, PosY = 6, PosZ = 21, Dir = 0, State = State.Idle };
                T_SpawnStatue(MonsterId.WolfPup, pos1);
                T_SpawnStatue(MonsterId.WolfPup, pos2);
                break;
            
            case 1: // 북 4
                PositionInfo pos3 = new() { PosX = 0, PosY = 6, PosZ = 18, Dir = 0, State = State.Idle };
                PositionInfo pos4 = new() { PosX = -3, PosY = 6, PosZ = 21, Dir = 0, State = State.Idle };
                T_SpawnStatue(MonsterId.Lurker, pos3);
                T_SpawnStatue(MonsterId.Snakelet, pos4);
                break;
            
            case 2:
                T_SkillUpgrade(Skill.WolfPupSpeed);
                T_SkillUpgrade(Skill.WolfPupHealth);
                T_SkillUpgrade(Skill.WolfPupAttack);
                T_SkillUpgrade(Skill.WolfPupAttackSpeed);
                break;
            
            case 3: // 북 4 남 1
                T_SkillUpgrade(Skill.LurkerSpeed);
                T_SkillUpgrade(Skill.LurkerHealth);
                T_SkillUpgrade(Skill.LurkerDefence);
                T_SkillUpgrade(Skill.LurkerHealth2);
                
                PositionInfo pos5 = new() { PosX = 0, PosY = 6, PosZ = -22, Dir = 0, State = State.Idle };
                T_SpawnStatue(MonsterId.WolfPup, pos5);
                
                
                break;
            
            case 4:
                T_SkillUpgrade(Skill.SnakeletAttack);
                T_SkillUpgrade(Skill.SnakeAttackSpeed);
                T_SkillUpgrade(Skill.SnakeletSpeed);
                T_SkillUpgrade(Skill.SnakeletRange);
                
                MonsterStatue? northWolfPup = _testStatues
                    .FirstOrDefault(statue => statue is { MonsterId: MonsterId.WolfPup, Way: SpawnWay.North });
                if (northWolfPup != null) T_UnitUpgrade(northWolfPup);
                break;
            
            case 5: 
                MonsterStatue? northLurker = _testStatues
                    .FirstOrDefault(statue => statue is { MonsterId: MonsterId.Lurker, Way: SpawnWay.North });
                if (northLurker != null) T_UnitUpgrade(northLurker);
                break;
            
            case 6:
                T_SkillUpgrade(Skill.WolfDefence);
                T_SkillUpgrade(Skill.WolfDrain);
                T_SkillUpgrade(Skill.WolfAvoid);
                T_SkillUpgrade(Skill.WolfCritical);
                T_SkillUpgrade(Skill.WolfFireResist);
                T_SkillUpgrade(Skill.WolfPoisonResist);
                T_SkillUpgrade(Skill.WolfDna);
                T_SkillUpgrade(Skill.SnakeAttack);
                T_SkillUpgrade(Skill.SnakeletAttackSpeed);
                T_SkillUpgrade(Skill.SnakeRange);
                T_SkillUpgrade(Skill.SnakeAccuracy);
                T_SkillUpgrade(Skill.SnakeFire);
                
                break;
            
            case 7:
                T_SkillUpgrade(Skill.CreeperSpeed);
                T_SkillUpgrade(Skill.CreeperAttackSpeed);
                T_SkillUpgrade(Skill.CreeperAttack);
                T_SkillUpgrade(Skill.CreeperRoll);
                T_SkillUpgrade(Skill.CreeperPoison);
                
                PositionInfo pos7 = new() { PosX = -3, PosY = 6, PosZ = 18, Dir = 0, State = State.Idle };
                T_SpawnStatue(MonsterId.Werewolf, pos7);
                
                MonsterStatue? northWolfpup = _testStatues
                    .FirstOrDefault(statue => statue is { MonsterId: MonsterId.WolfPup, Way: SpawnWay.North });
                if (northWolfpup != null) T_UnitUpgrade(northWolfpup);
                break;
            
            case 8:
                T_SkillUpgrade(Skill.WerewolfThunder);
                
                MonsterStatue? northCreeper = _testStatues
                    .FirstOrDefault(statue => statue is { MonsterId: MonsterId.Creeper, Way: SpawnWay.North });
                if (northCreeper != null) T_UnitUpgrade(northCreeper);
                break;
            
            case 9:
                T_SkillUpgrade(Skill.HorrorRollPoison);
                T_SkillUpgrade(Skill.HorrorPoisonStack);
                break;
            case 10:
                break;
            case 11:
                break;
            default: return;
        }
    }
}
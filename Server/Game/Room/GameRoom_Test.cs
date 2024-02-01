using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    List<MonsterStatue> _testStatues = new();
    
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
        switch (round)
        {
            case 0:
                PositionInfo pos1 = new() { PosX = -2, PosY = 6, PosZ = 22, Dir = 0, State = State.Idle };
                PositionInfo pos2 = new() { PosX = 2, PosY = 6, PosZ = 22, Dir = 0, State = State.Idle };
                T_SpawnStatue(MonsterId.WolfPup, pos1);
                T_SpawnStatue(MonsterId.WolfPup, pos2);
                break;
            
            // case 1:
            //     PositionInfo pos3 = new() { PosX = 0, PosY = 6, PosZ = 21, Dir = 0, State = State.Idle };
            //     PositionInfo pos4 = new() { PosX = 0, PosY = 6, PosZ = 23, Dir = 0, State = State.Idle };
            //     T_SpawnStatue(MonsterId.Lurker, pos3);
            //     T_SpawnStatue(MonsterId.Snakelet, pos4);
            //     break;
            //
            // case 2:
            //     PositionInfo pos5 = new() { PosX = 0, PosY = 6, PosZ = -22, Dir = 0, State = State.Idle };
            //     T_SpawnStatue(MonsterId.WolfPup, pos5);
            //     T_SkillUpgrade(Skill.WolfPupSpeed);
            //     break;
            //
            // case 3:
            //     T_SkillUpgrade(Skill.WolfPupHealth);
            //     T_SkillUpgrade(Skill.WolfPupAttack);
            //     T_SkillUpgrade(Skill.WolfPupAttackSpeed);
            //     upgradeStatueList.AddRange(_testStatues
            //         .Where(statue => statue is { MonsterId: MonsterId.WolfPup, Way: SpawnWay.North }));
            //     foreach (var statue in upgradeStatueList)
            //     {
            //         T_UnitUpgrade(statue);
            //     }
            //     upgradeStatueList.Clear();
            //     break;
            //
            // case 4:
            //     T_SkillUpgrade(Skill.LurkerSpeed);
            //     T_SkillUpgrade(Skill.LurkerHealth);
            //     T_SkillUpgrade(Skill.SnakeletSpeed);
            //     T_SkillUpgrade(Skill.SnakeletRange);
            //     
            //     MonsterStatue? southWolfPup = _testStatues
            //         .FirstOrDefault(statue => statue is { MonsterId: MonsterId.WolfPup, Way: SpawnWay.South });
            //     if (southWolfPup != null) T_UnitUpgrade(southWolfPup);
            //     break;
            //
            // case 5:
            //     T_SkillUpgrade(Skill.LurkerDefence);
            //     T_SkillUpgrade(Skill.LurkerHealth2);
            //     T_SkillUpgrade(Skill.SnakeletAttack);
            //     T_SkillUpgrade(Skill.SnakeAttackSpeed);
            //     
            //     MonsterStatue? northSnakelet = _testStatues
            //         .FirstOrDefault(statue => statue is { MonsterId: MonsterId.Snakelet, Way: SpawnWay.North });
            //     if (northSnakelet != null) T_UnitUpgrade(northSnakelet);
            //     MonsterStatue? northLurker = _testStatues
            //         .FirstOrDefault(statue => statue is { MonsterId: MonsterId.Lurker, Way: SpawnWay.North });
            //     if (northLurker != null) T_UnitUpgrade(northLurker);
            //     
            //     PositionInfo pos6 = new() { PosX = 3, PosY = 6, PosZ = 22, Dir = 0, State = State.Idle };
            //     T_SpawnStatue(MonsterId.Shell, pos6);
            //     break;
            //
            // case 6:
            //     T_SkillUpgrade(Skill.ShellAttackSpeed);
            //     T_SkillUpgrade(Skill.ShellSpeed);
            //     T_SkillUpgrade(Skill.ShellHealth);
            //     T_SkillUpgrade(Skill.ShellRoll);
            //     
            //     PositionInfo pos7 = new() { PosX = 3, PosY = 6, PosZ = -23, Dir = 0, State = State.Idle };
            //     T_SpawnStatue(MonsterId.Snake, pos7);
            //     PositionInfo pos8 = new() { PosX = 1, PosY = 6, PosZ = 21, Dir = 0, State = State.Idle };
            //     T_SpawnStatue(MonsterId.Wolf, pos8);
            //     break;
            //
            // case 7:
            //     T_SkillUpgrade(Skill.CreeperSpeed);
            //     T_SkillUpgrade(Skill.CreeperAttackSpeed);
            //     T_SkillUpgrade(Skill.CreeperAttack);
            //     T_SkillUpgrade(Skill.CreeperRoll);
            //     T_SkillUpgrade(Skill.CreeperPoison);
            //     
            //     MonsterStatue? northCreeper = _testStatues
            //         .FirstOrDefault(statue => statue is { MonsterId: MonsterId.Creeper, Way: SpawnWay.North });
            //     if (northCreeper != null) T_UnitUpgrade(northCreeper);
            //     break;
            //
            // case 8:
            //     T_SkillUpgrade(Skill.WolfDefence);
            //     T_SkillUpgrade(Skill.WolfDrain);
            //     T_SkillUpgrade(Skill.WolfAvoid);
            //     T_SkillUpgrade(Skill.WolfCritical);
            //     T_SkillUpgrade(Skill.WolfFireResist);
            //     T_SkillUpgrade(Skill.WolfPoisonResist);
            //     T_SkillUpgrade(Skill.WolfDna);
            //     
            //     MonsterStatue? northWolf = _testStatues
            //         .FirstOrDefault(statue => statue is { MonsterId: MonsterId.Wolf, Way: SpawnWay.North });
            //     if (northWolf != null) T_UnitUpgrade(northWolf);
            //     break;
            //
            // case 9:
            //     T_SkillUpgrade(Skill.WerewolfThunder);
            //     T_SkillUpgrade(Skill.WerewolfDebuffResist);
            //     T_SkillUpgrade(Skill.WerewolfFaint);
            //
            //     PositionInfo pos9 = new() { PosX = 1, PosY = 0, PosZ = 23, Dir = 0, State = State.Idle };
            //     T_SpawnStatue(MonsterId.Snake, pos9);
            //     break;
            case 10:
                break;
            case 11:
                break;
            default: return;
        }
    }
}
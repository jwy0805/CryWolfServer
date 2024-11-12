using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    private void SpawnStatue(UnitId monsterId, PositionInfo pos)
    {
        var player = _npc;
        var statue = SpawnMonsterStatue(monsterId, pos, player);
        SpawnEffect(EffectId.Upgrade, statue, statue);
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
        SpawnEffect(EffectId.Upgrade, statue, statue);
    }
    
    private void SetTutorialStatues(int round)
    {
        TestCase0(round);
    }

    private void TestCase0(int round)
    {
        switch (round)
        {
            case 0:
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -5, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -3, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -1, PosY = 6, PosZ = 12 });
                break;
        }
    }
    
    private void TestCase1(int round)
    {
        switch (round)
        {
            case 0:
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -5, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -3, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -1, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = 1, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = 3, PosY = 6, PosZ = 12 });
                break;
            default:
                break;
        }
    }
    
    private void TestCase2(int round)
    {
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
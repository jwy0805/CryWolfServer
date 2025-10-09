using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    public void SpawnStatueForTest(UnitId monsterId, PositionInfo pos)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        SpawnMonsterStatue(monsterId, pos, npc);
    }
    
    public MonsterStatue SpawnStatue(UnitId monsterId, PositionInfo pos)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        var statue = SpawnMonsterStatue(monsterId, pos, npc);
        SpawnEffect(EffectId.Upgrade, statue, statue);
        
        return statue;
    }    

    public void SpawnTower(UnitId towerId, PositionInfo pos)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        var tower = SpawnTower(towerId, pos, npc);
        SpawnEffect(EffectId.Upgrade, tower, tower);
    }

    public Tower SpawnTowerOnRelativeZ(UnitId unitId, Vector3 relativePos)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        var pos = new PositionInfo
        {
            PosX = relativePos.X,
            PosY = relativePos.Y,
            PosZ = GameInfo.FenceStartPos.Z + relativePos.Z,
        };
        var tower = SpawnTower(unitId, pos, npc);
        SpawnEffect(EffectId.Upgrade, tower, tower);

        return tower;
    }  
    
    public void UpgradeSkill(Skill skill)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        if (npc == null) return;
        npc.SkillSubject.SkillUpgraded(skill);
        npc.SkillUpgradedList.Add(skill);
    }

    public void UpgradeBaseSkill(Skill skill, Player player)
    {
        if (_storage == null || _portal == null) return;
        
        switch (skill)
        {
            case Skill.RepairSheep:
                RepairFences(_fences.Values.ToList());
                break;
            case Skill.RepairWolf:
                RepairStatues(_statues.Values.ToList());
                break;
            case Skill.BaseUpgradeSheep:
                _storage.LevelUp();
                break;
            case Skill.BaseUpgradeWolf:
                _portal.LevelUp();
                break;
            case Skill.ResourceSheep:
                break;
            case Skill.ResourceWolf:
                break;
            case Skill.AssetSheep:
                SpawnSheep(player);
                break;
            case Skill.AssetWolf:
                if (Enchant is not { EnchantLevel: < 5 }) return;
                Enchant.EnchantLevel++;
                break;
        }
    }
    
    public void UpgradeUnit(GameObject gameObject, Player player)
    {
        var id = gameObject.Id;
        PositionInfo newPos = new()
        {
            PosX = gameObject.PosInfo.PosX,
            PosY = gameObject.PosInfo.PosY,
            PosZ = gameObject.PosInfo.PosZ,
        };

        GameObject newObject;
        if (gameObject.ObjectType == GameObjectType.Tower)
        {
            if (gameObject is not Tower tower) return;
            var hpPortion = tower.Hp / (float)tower.MaxHp;
            LeaveGame(id);
            newPos.State = gameObject.State != State.Die ? State.Idle : State.Die;
            newObject = SpawnTower(tower.UnitId + 1, newPos, player);
            newObject.Hp = (int)(newObject.MaxHp * hpPortion);
            
            if (newPos.State == State.Die)
            {
                newObject.Targetable = false;
                DieAndLeave(newObject.Id);
            }
            
            newObject.BroadcastState();
            var soundPacket = new S_PlaySound
                { ObjectId = newObject.Id, Sound = Sounds.UpgradeTower, SoundType = SoundType.D3 };
            Push(Broadcast, soundPacket);
        }
        else if (gameObject.ObjectType == GameObjectType.MonsterStatue)
        {
            if (gameObject is not MonsterStatue statue) return;
            LeaveGame(id);
            newObject = SpawnMonsterStatue(statue.UnitId + 1, newPos, player);
            
            var soundPacket = new S_PlaySound
                { ObjectId = newObject.Id, Sound = Sounds.UpgradeStatue, SoundType = SoundType.D3 };
            Push(Broadcast, soundPacket);
        }
        else if (gameObject.ObjectType == GameObjectType.Monster)
        {
            if (gameObject is not Monster monster) return;
            var statueId = monster.StatueId;
            if (FindGameObjectById(statueId) is not MonsterStatue) return;
            LeaveGame(statueId);
            newObject = SpawnMonster(monster.UnitId + 1, newPos, player);
        }
        else
        {
            return;
        }
        
        SpawnEffect(EffectId.Upgrade, newObject, newObject);
    }

    // Only for single play scenario
    public void UpgradeUnitSingle(Dictionary<string, Tower> towers, string key, Player player)
    {
        if (towers.TryGetValue(key, out var tower))
        {
            var id = tower.Id;
            PositionInfo newPos = new()
            {
                PosX = tower.PosInfo.PosX,
                PosY = tower.PosInfo.PosY,
                PosZ = tower.PosInfo.PosZ,
            };
            
            LeaveGame(id);
            var newObject = SpawnTower(tower.UnitId + 1, newPos, player);
            towers[key] = newObject;
        }
    }
    
    public void UpgradeStatue(Dictionary<string, MonsterStatue> statues, string key, Player player)
    {
        if (statues.TryGetValue(key, out var statue))
        {
            var id = statue.Id;
            PositionInfo newPos = new()
            {
                PosX = statue.PosInfo.PosX,
                PosY = statue.PosInfo.PosY,
                PosZ = statue.PosInfo.PosZ,
            };
            
            LeaveGame(id);
            var newObject = SpawnMonsterStatue(statue.UnitId + 1, newPos, player);
            statues[key] = newObject;
        }
    }

    private void TestCaseSheep0(int round)
    {
        switch (round)
        {
            case 0:
                // SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -5, PosY = 6, PosZ = 12 });
                // SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -3, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -1, PosY = 6, PosZ = 12 });
                break;
        }
    }
    
    private void TestCaseSheep1(int round)
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
    
    private void TestCaseSheep2(int round)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true});
        if (npc == null) return;
        
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
                UpgradeSkill(Skill.LurkerDefence);
                
                PositionInfo pos5 = new() { PosX = 1.5f, PosY = 6, PosZ = 13, State = State.Idle };
                SpawnStatue(UnitId.WolfPup, pos5);
                
                break;
            
            case 4:
                UpgradeSkill(Skill.SnakeletAttack);
                
                var northWolfPup1 = _statues.Values
                    .FirstOrDefault(statue => statue is { UnitId: UnitId.WolfPup, Way: SpawnWay.North });
                if (northWolfPup1 != null) UpgradeUnit(northWolfPup1, npc);
                break;
            
            case 5: 
                var northLurker = _statues.Values
                    .FirstOrDefault(statue => statue is { UnitId: UnitId.Lurker, Way: SpawnWay.North });
                if (northLurker != null) UpgradeUnit(northLurker, npc);
                break;
            
            case 6:
                UpgradeSkill(Skill.SnakeletAttackSpeed);
                UpgradeSkill(Skill.SnakeAccuracy);
                
                break;
            
            case 7:
                UpgradeSkill(Skill.CreeperRoll);
                
                PositionInfo pos7 = new() { PosX = -3, PosY = 6, PosZ = 12, State = State.Idle };
                SpawnStatue(UnitId.Werewolf, pos7);
                
                var northWolfPup2 = _statues.Values
                    .FirstOrDefault(statue => statue is { UnitId: UnitId.WolfPup, Way: SpawnWay.North });
                if (northWolfPup2 != null) UpgradeUnit(northWolfPup2, npc);
                break;
            
            case 8:
                var northCreeper = _statues.Values
                    .FirstOrDefault(statue => statue is { UnitId: UnitId.Creeper, Way: SpawnWay.North });
                if (northCreeper != null) UpgradeUnit(northCreeper, npc);
                break;
            
            case 9:
                UpgradeSkill(Skill.HorrorRollPoison);
                break;
            case 10:
                break;
            case 11:
                break;
            default: return;
        }
    }

    private void TestCaseSheep3(int round)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        if (npc == null) return;
        
        switch (round)
        {
            case 0:
                var pos1 = new PositionInfo { PosX = 0, PosY = 6, PosZ = 14 };
                var pos2 = new PositionInfo { PosX = 2, PosY = 6, PosZ = 14 };
                var pos3 = new PositionInfo { PosX = 4, PosY = 6, PosZ = 14 };
                var pos4 = new PositionInfo { PosX = 6, PosY = 6, PosZ = 14 };
                var pos5 = new PositionInfo { PosX = 2, PosY = 6, PosZ = 16 };
                var pos6 = new PositionInfo { PosX = 4, PosY = 6, PosZ = 16 };
                SpawnStatue(UnitId.DogPup, pos1);
                SpawnStatue(UnitId.DogPup, pos2);
                SpawnStatue(UnitId.DogPup, pos3);
                SpawnStatue(UnitId.DogPup, pos4);
                SpawnStatue(UnitId.Lurker, pos5);
                SpawnStatue(UnitId.Lurker, pos6);
                break;
            case 1:
                UpgradeSkill(Skill.DogPupEvasion);
                UpgradeSkill(Skill.DogPupSpeed);
                UpgradeSkill(Skill.DogPupAttackSpeed);
                break;
            case 2:
                var dogPupList = _statues.Values.Where(statue => statue.UnitId == UnitId.DogPup).ToList();
                foreach (var statue in dogPupList)
                {
                    UpgradeUnit(statue, npc);
                }
                break;
            case 3:
                UpgradeSkill(Skill.LurkerSpeed);
                UpgradeSkill(Skill.LurkerDefence);
                break;
            case 4:
                UpgradeSkill(Skill.LurkerPoisonResist);
                break;
            case 5:
                var lurkerList = _statues.Values.Where(statue => statue.UnitId == UnitId.Lurker).ToList();
                foreach (var statue in lurkerList)
                {
                    UpgradeUnit(statue, npc);
                }
                break;
            case 6: 
                break;
        }
    }

    private void TestCaseSheep4(int round)
    {
        switch (round)
        {
            case 0:
                var pos1 = new PositionInfo { PosX = -1.5f, PosY = 6, PosZ = 13 };
                var pos2 = new PositionInfo { PosX = 1.5f, PosY = 6, PosZ = 13 };
                var pos3 = new PositionInfo { PosX = 0, PosY = 6, PosZ = 14 };
                var pos4 = new PositionInfo { PosX = -1, PosY = 6, PosZ = 16 };
                var pos5 = new PositionInfo { PosX = 1, PosY = 6, PosZ = 16 };
                SpawnStatue(UnitId.Cacti, pos1);
                SpawnStatue(UnitId.Cacti, pos2);
                SpawnStatue(UnitId.WolfPup, pos3);
                SpawnStatue(UnitId.Snakelet, pos4);
                SpawnStatue(UnitId.Snakelet, pos5);
                break;
            case 1:
                break;
            case 2:
                break;
        }
    }
    
    private void TestCaseWolf0(int round)
    {
        var fencePosZ = GameInfo.FenceStartPos.Z;
        switch (round)
        {
            case 0:
                PositionInfo pos1 = new() { PosX = 0, PosY = 6, PosZ = fencePosZ + 2 };
                SpawnTower(UnitId.TargetDummy, pos1);
                break;
        }
    }

    private void TestCaseWolf1(int round)
    {
        var fencePosZ = GameInfo.FenceStartPos.Z;
        switch (round)
        {
            case 0:
                PositionInfo pos1 = new() { PosX = 0, PosY = 6, PosZ = fencePosZ + 2 };
                PositionInfo pos2 = new() { PosX = 4, PosY = 6, PosZ = fencePosZ + 2 };
                PositionInfo pos3 = new() { PosX = -4, PosY = 6, PosZ = fencePosZ + 2 };
                PositionInfo pos4 = new() { PosX = 0, PosY = 6, PosZ = fencePosZ -1 };
                SpawnTower(UnitId.TrainingDummy, pos1);
                SpawnTower(UnitId.TrainingDummy, pos2);
                SpawnTower(UnitId.TrainingDummy, pos3);
                SpawnTower(UnitId.Bloom, pos4);
                break;
        }
    }
}
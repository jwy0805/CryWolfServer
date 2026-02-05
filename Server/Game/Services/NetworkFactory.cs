using Google.Protobuf.Protocol;

namespace Server.Game;

public class NetworkFactory : INetworkFactory
{
    public Player CreatePlayer(GameRoom room, MatchSuccessPacketRequired required, Faction faction)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };
        var sheepCharacterName = required.SheepCharacterId.ToString();
        var wolfCharacterName = required.WolfCharacterId.ToString();

        player.Room = room;
        player.Faction = faction;
        player.Info.Name = faction == Faction.Sheep ? sheepCharacterName : wolfCharacterName;
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.CharacterId = faction == Faction.Sheep ? required.SheepCharacterId : required.WolfCharacterId;
        player.AssetId = faction == Faction.Sheep ? (int)required.SheepId : (int)required.EnchantId;
        player.WinRankPoint = faction == Faction.Sheep ? required.WinPointSheep : required.WinPointWolf;
        player.LoseRankPoint = faction == Faction.Sheep ? required.LosePointSheep : required.LosePointWolf;
        player.RankPoint = faction == Faction.Sheep ? required.SheepRankPoint : required.WolfRankPoint;
        player.UnitIds = faction == Faction.Sheep ? required.SheepUnitIds : required.WolfUnitIds;
        player.Session = faction == Faction.Sheep 
            ? SessionManager.Instance.Find(required.SheepSessionId)
            : SessionManager.Instance.Find(required.WolfSessionId);

        Console.WriteLine($"Create Player -> {room.RoomId} {required.SheepSessionId} : {required.WolfSessionId}" );
        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        var userId = faction == Faction.Sheep ? required.SheepUserId : required.WolfUserId;
        player.Session.MyPlayer = player;
        player.Session.MarkAuthenticated(userId);
        
        return player;
    }
    
    public Player CreatePlayerFriendly(GameRoom room, FriendlyMatchPacketRequired required, Faction faction)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };
        var sheepCharacterName = required.SheepCharacterId.ToString();
        var wolfCharacterName = required.WolfCharacterId.ToString();

        player.Room = room;
        player.Faction = faction;
        player.Info.Name = faction == Faction.Sheep ? sheepCharacterName : wolfCharacterName;
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.CharacterId = faction == Faction.Sheep ? required.SheepCharacterId : required.WolfCharacterId;
        player.AssetId = faction == Faction.Sheep ? (int)required.SheepId : (int)required.EnchantId;
        player.UnitIds = faction == Faction.Sheep ? required.SheepUnitIds : required.WolfUnitIds;
        player.Session = faction == Faction.Sheep 
            ? SessionManager.Instance.Find(required.SheepSessionId)
            : SessionManager.Instance.Find(required.WolfSessionId);
        
        Console.WriteLine($"Create Player -> {room.RoomId} {required.SheepSessionId} : {required.WolfSessionId}" );
        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        var userId = faction == Faction.Sheep ? required.SheepUserId : required.WolfUserId;
        player.Session.MyPlayer = player;
        player.Session.MarkAuthenticated(userId);

        return player;
    }
    
    public Player CreatePlayerSingle(GameRoom room, SinglePlayStartPacketRequired required)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var faction = required.UserFaction;
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };
        
        player.Room = room;
        player.Faction = faction;
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.Info.Name = ((CharacterId)required.CharacterId).ToString();
        player.CharacterId = (CharacterId)required.CharacterId;
        player.AssetId = required.AssetId;
        player.UnitIds = required.UnitIds;
        player.Session = SessionManager.Instance.Find(required.SessionId);

        Console.WriteLine($"{required.SessionId} single play, room {room.RoomId}, {required.CharacterId} {required.EnemyCharacterId}");
        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        player.Session.MyPlayer = player;
        player.Session.MarkAuthenticated(required.UserId);

        return player;
    }
    
    public Player CreatePlayerTutorial(GameRoom room, TutorialStartPacketRequired required)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var faction = required.UserFaction;
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };

        player.Room = room;
        player.Faction = faction;
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.Info.Name = ((CharacterId)required.CharacterId).ToString();
        player.CharacterId = (CharacterId)required.CharacterId;
        player.AssetId = required.AssetId;
        player.UnitIds = required.UnitIds;
        player.Session = SessionManager.Instance.Find(required.SessionId);

        Console.WriteLine($"{required.SessionId} in tutorial");
        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        player.Session.MyPlayer = player;
        player.Session.MarkAuthenticated(required.UserId);

        return player;
    }

    public Player CreateNpc(GameRoom room, Player player, CharacterId characterId, int assetId, UnitId[]? unitIds = null)
    {
        unitIds ??= Array.Empty<UnitId>();
        // This is a test NPC, so this has to be changed later when the single play mode is implemented.
        var npc = ObjectManager.Instance.Add<Player>();
        var faction = player.Faction == Faction.Sheep ? Faction.Wolf : Faction.Sheep;
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };

        npc.Faction = faction;
        npc.Info.Name = characterId.ToString();
        npc.PosInfo = position;
        npc.Info.PosInfo = position;
        npc.CharacterId = characterId;
        npc.AssetId = assetId;
        npc.UnitIds = unitIds;
        room.EnterGameNpc(npc);

        Console.WriteLine($"Create NPC -> {npc.Info.Name}");
        return npc;
    }

    public void CreateNpcForAiGame(GameRoom room, Faction faction, CharacterId characterId, int assetId)
    {
        var npc = ObjectManager.Instance.Add<Player>();
        npc.Faction = faction;
        npc.Info.Name = characterId.ToString();
        npc.CharacterId = characterId;
        npc.AssetId = assetId;
        npc.UnitIds = room.GetAiDeck(faction);
        room.EnterGameNpc(npc);
    }
    
    public void CreateNpcForAiGame(GameRoom room, Faction faction, int sessionId, CharacterId characterId, int assetId)
    {
        var npc = ObjectManager.Instance.Add<Player>();
        npc.Faction = faction;
        npc.Info.Name = characterId.ToString();
        npc.CharacterId = characterId;
        npc.AssetId = assetId;
        npc.UnitIds = room.GetAiDeck(faction);
        npc.Session = SessionManager.Instance.Find(sessionId);
        room.EnterGameNpc(npc);
        if (npc.Session == null)
        {
            Console.WriteLine($"Session not found for user : {npc.Session?.UserId}");
            return;
        }
        npc.Session.MyPlayer = npc;
        npc.Session.MarkAuthenticated(npc.Session.UserId);
        Console.WriteLine($"Create NPC");
    }
}
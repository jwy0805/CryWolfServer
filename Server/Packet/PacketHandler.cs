using System.Numerics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;
using ServerCore;

public class PacketHandler
{
    public static void C_EnterGameHandler(PacketSession session, IMessage packet)
    {
        var enterPacket = (C_EnterGame)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        if (player != null) player.Camp = enterPacket.IsSheep ? Camp.Sheep : Camp.Wolf;    
    }
    
    public static async void C_SetSessionHandler(PacketSession session, IMessage packet)
    {
        var sessionPacket = (C_SetSession)packet;
        var clientSession = (ClientSession)session;
        
        if (sessionPacket.Test)
        {
            var playerPos = sessionPacket.Camp == Camp.Sheep 
                ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 } 
                : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };
            
            clientSession.MyPlayer = ObjectManager.Instance.Add<Player>();
            clientSession.MyPlayer.Info.Name = $"Player_{clientSession.MyPlayer.Info.ObjectId}";
            clientSession.MyPlayer.Info.PosInfo = playerPos;
            clientSession.MyPlayer.Session = clientSession;
        
            GameLogic.Instance.Push(() =>
            {
                var room = GameLogic.Instance.CreateGameRoom(1, true);
                room.Push(room.EnterGame, clientSession.MyPlayer);
            });
        }
        else
        {
            var webPacket = new GetUserIdPacketRequired { UserAccount = sessionPacket.UserAccount };
            var task = NetworkManager.Instance.GetUserIdFromApiServer<GetUserIdPacketResponse>(
                "/GetUserIdByAccount", webPacket);
        
            await task;
            if (task.Result == null) return;
            clientSession.UserId = task.Result.UserId;
        }
    }
    
    public static void C_SpawnHandler(PacketSession session, IMessage packet)
    {
        var spawnPacket = (C_Spawn)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSpawn, player, spawnPacket);
        Console.WriteLine($"{spawnPacket.Num}, {spawnPacket.PosInfo}");
    }
    
    public static void C_PlayerMoveHandler(PacketSession session, IMessage packet)
    {
        var pMovePacket = (C_PlayerMove)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandlePlayerMove, player, pMovePacket);
    }
    
    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        
    }
    
    public static void C_StateHandler(PacketSession session, IMessage packet)
    {
        var statePacket = (C_State)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandleState, player, statePacket);
    }
    
    public static void C_EffectActivateHandler(PacketSession session, IMessage packet)
    {
        var dirPacket = (C_EffectActivate)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleEffectActivate, player, dirPacket);
    }
    
    public static void C_BaseSkillRunHandler(PacketSession session, IMessage packet)
    {
        var skillPacket = (C_BaseSkillRun)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleBaseSkillRun, player, skillPacket);
    }
    
    public static void C_SkillUpgradeHandler(PacketSession session, IMessage packet)
    {
        var upgradePacket = (C_SkillUpgrade)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandleSkillUpgrade, player, upgradePacket);
    }

    public static void C_PortraitUpgradeHandler(PacketSession session, IMessage packet)
    {
        var upgradePacket = (C_PortraitUpgrade)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandlePortraitUpgrade, player, upgradePacket);
    }

    public static void C_UnitUpgradeHandler(PacketSession session, IMessage packet)
    {
        var upgradePacket = (C_UnitUpgrade)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleUnitUpgrade, player, upgradePacket);
    }
    
    public static void C_ChangeResourceHandler(PacketSession session, IMessage packet)
    {
        var resourcePacket = (C_ChangeResource)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandleChangeResource, player, resourcePacket);
    }
    
    public static void C_LeaveHandler(PacketSession session, IMessage packet)
    {
        var leavePacket = (C_Leave)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleLeave, player, leavePacket);
    }

    public static void C_UnitSpawnPosHandler(PacketSession session, IMessage packet)
    {
        var spawnPacket = (C_UnitSpawnPos)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        if (room == null) return;
        
        DestVector dest = spawnPacket.DestVector;
        Vector3 vector = new Vector3(dest.X, dest.Y, dest.Z);
        Vector2Int cellPos = room.Map.Vector3To2(vector);
        if (DataManager.UnitDict.TryGetValue(spawnPacket.UnitId, out var unitData) == false) return;
        Enum.TryParse(unitData.camp, out Camp camp);
        GameObjectType type = camp == Camp.Sheep ? GameObjectType.Tower : GameObjectType.Monster;
        
        var size = room.UnitSizeList.FirstOrDefault(s => s.UnitId == (UnitId)spawnPacket.UnitId).SizeX;
        var canSpawn = room.Map.CanSpawn(cellPos, size) && cellPos.Z > room.GameInfo.FenceStartPos.Z;
        
        var unitSpawnPacket = new S_UnitSpawnPos { CanSpawn = canSpawn, ObjectType = type };
        player?.Session?.Send(unitSpawnPacket);
    }

    public static void C_GetRangesHandler(PacketSession session, IMessage packet)
    {
        var rangePacket = (C_GetRanges)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        if (room == null) return;

        DataManager.UnitDict.TryGetValue(rangePacket.UnitId, out var unitData);
        if (unitData == null) return;
        
        var sendRangePacket = new S_GetRanges
        {
            AttackRange = unitData.stat.AttackRange, SkillRange = unitData.stat.SkillRange
        };
        player?.Session?.Send(sendRangePacket);
    }
    
    public static void C_SetTextUIHandler(PacketSession session, IMessage packet)
    {
        var uiPacket = (C_SetTextUI)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        if (uiPacket.Init) room?.InfoInit();
    }

    public static void C_DeleteUnitHandler(PacketSession session, IMessage packet)
    {
        var deletePacket = (C_DeleteUnit)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleDelete, player, deletePacket);
    }

    public static void C_SetUpgradePopupHandler(PacketSession session, IMessage packet)
    {
        var popupPacket = (C_SetUpgradePopup)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSetUpgradePopup, player, popupPacket);
    }

    public static void C_SetUpgradeButtonHandler(PacketSession session, IMessage packet)
    {
        var buttonPacket = (C_SetUpgradeButton)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSetUpgradeButton, player, buttonPacket);
    }
}
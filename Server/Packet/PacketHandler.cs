using System.Numerics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;
using ServerCore;

public class PacketHandler
{
    public static void C_StartGameSceneHandler(PacketSession session, IMessage packet)
    {
        var startPacket = (C_StartGameScene)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        if (room == null || player == null)
        {
            Console.WriteLine("Room or player is null");
            return;
        }

        var npc = room.FindPlayer(go => go is Player { IsNpc: true });
        if (npc != null)
        {
            room.Push(room.EnterGame, npc);
        }

        room.Push(room.EnterGame, player);
    }

    public static void C_HoldGameHandler(PacketSession session, IMessage packet)
    {
        var holdPacket = (C_HoldGame)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        if (room == null)
        {
            Console.WriteLine("Room is null");
            return;
        }
        
        room.RoomActivated = !holdPacket.Hold;
    }
    
    public static void C_SpawnHandler(PacketSession session, IMessage packet)
    {
        var spawnPacket = (C_Spawn)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSpawn, player, spawnPacket);
    }

    public static void C_BindStatueInfoHandler(PacketSession session, IMessage packet)
    {
        var bindPacket = (C_BindStatueInfo)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleBindStatueInfo, player, bindPacket);
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

    public static void C_UnitUpgradeHandler(PacketSession session, IMessage packet)
    {
        var upgradePacket = (C_UnitUpgrade)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleUnitUpgrade, player, upgradePacket);
    }
    
    public static void C_UnitRepairHandler(PacketSession session, IMessage packet)
    {
        var repairPacket = (C_UnitRepair)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleUnitRepair, player, repairPacket);
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
        Enum.TryParse(unitData.faction, out Faction faction);
        GameObjectType type = faction == Faction.Sheep ? GameObjectType.Tower : GameObjectType.Monster;
        
        var size = room.UnitSizeList.FirstOrDefault(s => s.UnitId == (UnitId)spawnPacket.UnitId).SizeX;
        var canSpawn = room.Map.CanSpawn(cellPos, size)
                       && room.Map.Vector2To3(cellPos).Z >= room.GameInfo.GetSpawnRangeMinZ(room, faction) 
                       && room.Map.Vector2To3(cellPos).Z <= room.GameInfo.GetSpawnRangeMaxZ(room, faction);
        
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

    public static void C_GetSpawnableBoundsHandler(PacketSession session, IMessage packet)
    {
        var boundsPacket = (C_GetSpawnableBounds)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        if (room == null) return;

        var sendBoundsPacket = new S_GetSpawnableBounds
        {
            MinZ = room.GameInfo.GetSpawnRangeMinZ(room, boundsPacket.Faction),
            MaxZ = room.GameInfo.GetSpawnRangeMaxZ(room, boundsPacket.Faction)
        };
        player?.Session?.Send(sendBoundsPacket);
    }
    
    public static void C_SetTextUIHandler(PacketSession session, IMessage packet)
    {
        var uiPacket = (C_SetTextUI)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        if (uiPacket.Init && player != null) room?.Push(room.InfoInit, player);
    }

    public static void C_UnitDeleteHandler(PacketSession session, IMessage packet)
    {
        var deletePacket = (C_UnitDelete)packet;
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

    public static void C_SetUpgradeCostHandler(PacketSession session, IMessage packet)
    {
        var buttonPacket = (C_SetUpgradeCost)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSetCostInUpgradeButton, player, buttonPacket);
    }
    
    public static void C_SetUnitDeleteCostHandler(PacketSession session, IMessage packet)
    {
        var deletePacket = (C_SetUnitDeleteCost)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSetDeleteCostText, player, deletePacket);
    }
    
    public static void C_SetUnitRepairCostHandler(PacketSession session, IMessage packet)
    {
        var repairPacket = (C_SetUnitRepairCost)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSetRepairCostText, player, repairPacket);
    }
    
    public static void C_SetBaseSkillCostHandler(PacketSession session, IMessage packet)
    {
        var costPacket = (C_SetBaseSkillCost)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSetBaseSkillCost, player, costPacket);
    }
}
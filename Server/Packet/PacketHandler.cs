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
    
    public static void C_SpawnHandler(PacketSession session, IMessage packet)
    {
        var spawnPacket = (C_Spawn)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandleSpawn, player, spawnPacket);
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
        var movePacket = (C_Move)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandleMove, player, movePacket);
    }
    
    public static void C_StateHandler(PacketSession session, IMessage packet)
    {
        var statePacket = (C_State)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandleState, player, statePacket);
    }
    
    public static void C_SetDestHandler(PacketSession session, IMessage packet)
    {
        var destPacket = (C_SetDest)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandleSetDest, player, destPacket);
    }

    public static void C_AttackHandler(PacketSession session, IMessage packet)
    {
        var attackPacket = (C_Attack)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        if (player?.Session.SessionId != 1) return;
        room?.Push(room.HandleAttack, player, attackPacket);
    }

    public static void C_EffectAttackHandler(PacketSession session, IMessage packet)
    {
        var dirPacket = (C_EffectAttack)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleEffectAttack, player, dirPacket);
    }
    
    public static void C_StatInitHandler(PacketSession session, IMessage packet)
    {
        var initPacket = (C_StatInit)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleStatInit, player, initPacket);
    }

    public static void C_SkillInitHandler(PacketSession session, IMessage packet)
    {
        var initPacket = (C_SkillInit)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;
        
        room?.Push(room.HandleSkillInit, player, initPacket);    
    }
    
    public static void C_SkillHandler(PacketSession session, IMessage packet)
    {
        var skillPacket = (C_Skill)packet;
        var clientSession = (ClientSession)session;
        var player = clientSession.MyPlayer;
        var room = player?.Room;

        room?.Push(room.HandleSkill, player, skillPacket);
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
        
        int size = room.UnitSizeList.FirstOrDefault(s => s.UnitId == (UnitId)spawnPacket.UnitId).SizeX;
        bool canSpawn = room.Map.CanSpawn(cellPos, size);
        
        var unitSpawnPacket = new S_UnitSpawnPos { CanSpawn = canSpawn, ObjectType = type};
        room.Broadcast(unitSpawnPacket);
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
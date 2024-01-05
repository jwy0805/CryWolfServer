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
        C_EnterGame enterPacket = (C_EnterGame)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        if (player != null) player.Camp = enterPacket.IsSheep ? Camp.Sheep : Camp.Wolf;    
    }
    
    public static void C_SpawnHandler(PacketSession session, IMessage packet)
    {
        C_Spawn spawnPacket = (C_Spawn)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandleSpawn, player, spawnPacket);
    }
    
    public static void C_PlayerMoveHandler(PacketSession session, IMessage packet)
    {
        C_PlayerMove pMovePacket = (C_PlayerMove)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandlePlayerMove, player, pMovePacket);
    }
    
    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        C_Move movePacket = (C_Move)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandleMove, player, movePacket);
    }
    
    public static void C_StateHandler(PacketSession session, IMessage packet)
    {
        C_State statePacket = (C_State)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandleState, player, statePacket);
    }
    
    public static void C_SetDestHandler(PacketSession session, IMessage packet)
    {
        C_SetDest destPacket = (C_SetDest)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandleSetDest, player, destPacket);
    }

    public static void C_AttackHandler(PacketSession session, IMessage packet)
    {
        C_Attack attackPacket = (C_Attack)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        if (player?.Session.SessionId != 1) return;
        room?.Push(room.HandleAttack, player, attackPacket);
    }

    public static void C_EffectAttackHandler(PacketSession session, IMessage packet)
    {
        C_EffectAttack dirPacket = (C_EffectAttack)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        
        room?.Push(room.HandleEffectAttack, player, dirPacket);
    }
    
    public static void C_StatInitHandler(PacketSession session, IMessage packet)
    {
        C_StatInit initPacket = (C_StatInit)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        
        room?.Push(room.HandleStatInit, player, initPacket);
    }

    public static void C_SkillInitHandler(PacketSession session, IMessage packet)
    {
        C_SkillInit initPacket = (C_SkillInit)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        
        room?.Push(room.HandleSkillInit, player, initPacket);    
    }
    
    public static void C_SkillHandler(PacketSession session, IMessage packet)
    {
        C_Skill skillPacket = (C_Skill)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandleSkill, player, skillPacket);
    }

    public static void C_SkillUpgradeHandler(PacketSession session, IMessage packet)
    {
        C_SkillUpgrade upgradePacket = (C_SkillUpgrade)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandleSkillUpgrade, player, upgradePacket);
    }

    public static void C_PortraitUpgradeHandler(PacketSession session, IMessage packet)
    {
        C_PortraitUpgrade upgradePacket = (C_PortraitUpgrade)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandlePortraitUpgrade, player, upgradePacket);
    }

    public static void C_UnitUpgradeHandler(PacketSession session, IMessage packet)
    {
        C_UnitUpgrade upgradePacket = (C_UnitUpgrade)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        
        room?.Push(room.HandleUnitUpgrade, player, upgradePacket);
    }
    
    public static void C_ChangeResourceHandler(PacketSession session, IMessage packet)
    {
        C_ChangeResource resourcePacket = (C_ChangeResource)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;

        room?.Push(room.HandleChangeResource, player, resourcePacket);
    }
    
    public static void C_LeaveHandler(PacketSession session, IMessage packet)
    {
        C_Leave leavePacket = (C_Leave)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        
        room?.Push(room.HandleLeave, player, leavePacket);
    }

    public static void C_TowerSpawnPosHandler(PacketSession session, IMessage packet)
    {
        C_TowerSpawnPos spawnPacket = (C_TowerSpawnPos)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        if (room == null) return;
        
        DestVector dest = spawnPacket.DestVector;
        Vector3 vector = new Vector3(dest.X, dest.Y, dest.Z);
        Vector2Int cellPos = room.Map.Vector3To2(vector);

        int size = room.TowerSizeList.FirstOrDefault(s => s.TowerId == (TowerId)spawnPacket.TowerId).SizeX;
        bool canSpawn = room.Map.CanSpawn(cellPos, size);
        
        S_TowerSpawnPos towerSpawnPacket = new S_TowerSpawnPos { CanSpawn = canSpawn };
        room.Broadcast(towerSpawnPacket);
    }

    public static void C_MonsterSpawnPosHandler(PacketSession session, IMessage packet)
    {
        C_MonsterSpawnPos spawnPacket = (C_MonsterSpawnPos)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        if (room == null) return;
        
        DestVector dest = spawnPacket.DestVector;
        Vector3 vector = new Vector3(dest.X, dest.Y, dest.Z);
        Vector2Int cellPos = room.Map.Vector3To2(vector);

        int size = room.MonsterSizeList.FirstOrDefault(s => s.MonsterId == (MonsterId)spawnPacket.MonsterId).SizeX;
        bool canSpawn = room.Map.CanSpawn(cellPos, size);
        
        S_MonsterSpawnPos monsterSpawnPacket = new S_MonsterSpawnPos { CanSpawn = canSpawn };
        room.Broadcast(monsterSpawnPacket);
    }
    
    public static void C_SetTextUIHandler(PacketSession session, IMessage packet)
    {
        C_SetTextUI uiPacket = (C_SetTextUI)packet;
        ClientSession clientSession = (ClientSession)session;
        Player? player = clientSession.MyPlayer;
        GameRoom? room = player?.Room;
        if (uiPacket.Init) room?.InfoInit();
    }
}
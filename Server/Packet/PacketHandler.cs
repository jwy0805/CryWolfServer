using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Game;
using ServerCore;

public class PacketHandler
{
    public static void C_SpawnHandler(PacketSession session, IMessage packet)
    {
        C_Spawn spawnPacket = (C_Spawn)packet;
        ClientSession clientSession = (ClientSession)session;
        
        Player? player = clientSession.MyPlayer;
        if (player == null) return;
        GameRoom? room = player.Room;
        if (room == null) return;
        
        room.Push(room.HandleSpawn, player, spawnPacket);
    }
    
    public static void C_PlayerMoveHandler(PacketSession session, IMessage packet)
    {
        C_PlayerMove pMovePacket = (C_PlayerMove)packet;
        ClientSession clientSession = (ClientSession)session;

        Player? player = clientSession.MyPlayer;
        if (player == null) return;
        GameRoom? room = player.Room;
        if (room == null) return;
        
        room.Push(room.HandlePlayerMove, player, pMovePacket);
    }
    
    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        C_Move movePacket = (C_Move)packet;
        ClientSession clientSession = (ClientSession)session;

        Console.WriteLine($"C_Move ({movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosY}, {movePacket.PosInfo.PosZ})");

        Player? player = clientSession.MyPlayer;
        if (player == null) return;
        GameRoom? room = player.Room;
        if (room == null) return;
        
        room.Push(room.HandleMove, player, movePacket);
    }

    public static void C_SetDestHandler(PacketSession session, IMessage packet)
    {
        C_SetDest destPacket = (C_SetDest)packet;
        ClientSession clientSession = (ClientSession)session;
        
        Player? player = clientSession.MyPlayer;
        if (player == null) return;
        GameRoom? room = player.Room;
        if (room == null) return;
        
        room.Push(room.HandleSetDest, player, destPacket);
    }
    
    public static void C_SkillHandler(PacketSession session, IMessage packet)
    {
        C_Skill skillPacket = (C_Skill)packet;
        ClientSession serverSession = (ClientSession)session;
    }
}
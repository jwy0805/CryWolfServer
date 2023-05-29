using DummyClient;
using ServerCore;

public class PacketHandler
{
    public static void S_BroadcastEnterGameHandler(PacketSession session, IPacket packet)
    {
        S_BroadcastEnterGame pkt = packet as S_BroadcastEnterGame;
        ServerSession serverSession = session as ServerSession;

        // if (chatPacket.playerId == 1) 
            // Console.WriteLine(chatPacket.chat);
    }

    public static void S_BroadcastLeaveGameHandler(PacketSession session, IPacket packet)
    {
        
    }
    
    public static void S_PlayerListHandler(PacketSession session, IPacket packet)
    {
        
    }
    
    public static void S_BroadcastMoveHandler(PacketSession session, IPacket packet)
    {
        
    }
}
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using ServerCore;

public class PacketHandler
{
    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        C_Move movePacket = (C_Move)packet;
        ClientSession clientSession = (ClientSession)session;

        Console.WriteLine($"C_Move ({movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosZ})");

        if (clientSession.MyPlayer == null) return;
        if (clientSession.MyPlayer.Room == null) return;

        ObjectInfo info = clientSession.MyPlayer.Info;
        info.PosInfo = movePacket.PosInfo;

        S_Move responseMovePacket = new S_Move
        {
            ObjectId = clientSession.MyPlayer.Info.ObjectId,
            PosInfo = movePacket.PosInfo
        };

        clientSession.MyPlayer.Room.Broadcast(responseMovePacket);
    }

    public static void C_SkillHandler(PacketSession session, IMessage packet)
    {
        C_Skill skillPacket = (C_Skill)packet;
        ClientSession serverSession = (ClientSession)session;
    }
}
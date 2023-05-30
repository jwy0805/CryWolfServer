using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using ServerCore;

public class PacketHandler
{
    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        C_Move movePacket = (C_Move)packet;
        ClientSession serverSession = (ClientSession)session;
    }

    public static void C_SkillHandler(PacketSession session, IMessage packet)
    {
        C_Skill skillPacket = (C_Skill)packet;
        ClientSession serverSession = (ClientSession)session;
    }
}
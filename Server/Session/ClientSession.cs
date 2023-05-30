using System.Net;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using ServerCore;

namespace Server;

public class ClientSession : PacketSession
{
    public Player MyPlayer { get; set; }
    public int SessionId { get; set; }

    public void Send(IMessage packet)
    {
        string messageName = packet.Descriptor.Name.Replace("_", string.Empty);
        MessageId messageId = (MessageId)Enum.Parse(typeof(MessageId), messageName);
        ushort size = (ushort)packet.CalculateSize();
        byte[] sendBuffer = new byte[size + 4];
        Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
        Array.Copy(BitConverter.GetBytes((ushort)messageId), 0, sendBuffer, 2, sizeof(ushort));
        Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
        Send(new ArraySegment<byte>(sendBuffer));
    }
    
    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnConnected : {endPoint}");

        MyPlayer = PlayerManager.Instance.Add();
        {
            MyPlayer.Info.Name = $"Player_{MyPlayer.Info.ObjectId}";
            MyPlayer.Info.PosInfo.State = CreatureState.Idle;
            MyPlayer.Info.PosInfo.PosX = 0f;
            MyPlayer.Info.PosInfo.PosY = 6f;
            MyPlayer.Info.PosInfo.PosZ = 0f;
            MyPlayer.Info.PosInfo.RotY = 0f;
        }
        
        // TODO :  RoomId 받아서 맞는 룸에 들어갈 수 있도록
        RoomManager.Instance.Find(1)?.EnterGame(MyPlayer);
        
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        PacketManager.Instance.OnRecvPacket(this, buffer);
    }
    
    public override void OnDisconnected(EndPoint endPoint)
    {
        RoomManager.Instance.Find(1)?.LeaveGame(MyPlayer.Info.ObjectId);
        SessionManager.Instance.Remove(this);
        Console.WriteLine($"OnDisconnected : {endPoint}");
    }

    public override void OnSend(int numOfBytes)
    {
        
    }
}
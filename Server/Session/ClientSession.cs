using System.Net;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using ServerCore;
using GameRoom = Server.Game.GameRoom;

namespace Server;

public class ClientSession : PacketSession
{
    public Player? MyPlayer { get; set; }
    public int SessionId { get; set; }
    public int UserId { get; set; }

    private readonly object _lock = new();
    private List<ArraySegment<byte>> _reserveQueue = new();
    
    public void Send(IMessage packet)
    {
        string messageName = packet.Descriptor.Name.Replace("_", string.Empty);
        MessageId messageId = (MessageId)Enum.Parse(typeof(MessageId), messageName);
        ushort size = (ushort)packet.CalculateSize();
        byte[] sendBuffer = new byte[size + 4];
        Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
        Array.Copy(BitConverter.GetBytes((ushort)messageId), 0, sendBuffer, 2, sizeof(ushort));
        Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);

        lock (_lock)
        {
            _reserveQueue.Add(sendBuffer);
        }
    }

    public void FlushSend()
    {
        List<ArraySegment<byte>> sendList;

        lock (_lock)
        {
            if (_reserveQueue.Count == 0) return;

            sendList = _reserveQueue;
            _reserveQueue = new List<ArraySegment<byte>>();
        }
        
        Send(sendList);
    }
    
    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnConnected : {endPoint}, {SessionId}");
        Send(new S_ConnectSession { SessionId = SessionId });
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        PacketManager.Instance.OnRecvPacket(this, buffer);
    }
    
    public override void OnDisconnected(EndPoint endPoint)
    {
        // GameLogic.Instance.Push(() =>
        // {
        //     GameRoom? room = GameLogic.Instance.FindByUserId(UserId);
        //     room?.Push(room.LeaveGame, MyPlayer.Info.ObjectId);
        // });
        NetworkManager.Instance.OnSessionDisconnected(SessionId);
        SessionManager.Instance.Remove(this);
        Console.WriteLine($"OnDisconnected : {endPoint}");
    }

    public override void OnSend(int numOfBytes)
    {
        
    }
}
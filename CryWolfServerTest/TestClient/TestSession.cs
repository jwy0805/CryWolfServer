using System.Diagnostics;
using System.Net;
using ServerCore;

namespace CryWolfServerTest.TestClient;

public class TestSession : PacketSession
{
    public int SessionId { get; set; }
    
    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"Connected to {endPoint}");
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        Console.WriteLine($"Disconnected from {endPoint}");
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        PacketManager.Instance.OnRecvPacket(this, buffer);
    }
    
    public override void OnSend(int numOfBytes)
    {
        
    }
}
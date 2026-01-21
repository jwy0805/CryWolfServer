using System.Diagnostics;
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
    
    // ---- health-check / user-session 분기용 상태 ----
    private readonly Stopwatch _life = Stopwatch.StartNew();
    private volatile int _recvPackets;
    private volatile int _recvBytes;
    private volatile bool _authenticated;
    private EndPoint? _remoteEndPoint;
    
    private static readonly TimeSpan HealthCheckDisconnectThreshold = TimeSpan.FromSeconds(1.2);

    public void MarkAuthenticated(int userId)
    {
        UserId = userId;
        _authenticated = true;

        // 유저 세션 로그 - 인증 완료 시점에
        Console.WriteLine($"[USER_CONNECTED] SessionId={SessionId} UserId={UserId} Remote={_remoteEndPoint}");
    }
    
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
        _remoteEndPoint = endPoint;
        Send(new S_ConnectSession { SessionId = SessionId });
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        Interlocked.Increment(ref _recvPackets);        
        Interlocked.Add(ref _recvBytes, buffer.Count);
        
        PacketManager.Instance.OnRecvPacket(this, buffer);
    }
    
    public override void OnDisconnected(EndPoint endPoint)
    {
        _ = NetworkManager.Instance.OnSessionDisconnected(SessionId);
        SessionManager.Instance.Remove(this);

        var elapsed = _life.Elapsed;

        // 유저 세션 로그
        if (_authenticated)
        {
            Console.WriteLine($"[USER_DISCONNECTED] SessionId={SessionId} UserId={UserId} Remote={endPoint} " +
                              $"LifetimeMs={elapsed.TotalMilliseconds:F0} RecvPackets={_recvPackets} RecvBytes={_recvBytes}");
            return;
        }

        // 헬스체크 로그
        if (_recvPackets == 0 && elapsed <= HealthCheckDisconnectThreshold) return;

        // 그 외
        Console.WriteLine($"[DISCONNECTED_PREAUTH] SessionId={SessionId} Remote={endPoint} " +
                          $"LifetimeMs={elapsed.TotalMilliseconds:F0} RecvPackets={_recvPackets} RecvBytes={_recvBytes}");
    }

    public override void OnSend(int numOfBytes)
    {
        
    }
}
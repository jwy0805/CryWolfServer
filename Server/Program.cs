using System.Net;
using System.Net.Sockets;
using System.Text;
using Server.Game;
using ServerCore;

namespace Server;

public class Program
{
    private static Listener _listener = new Listener();

    private static void FlushRoom()
    {
        JobTimer.Instance.Push(FlushRoom, 250);
    }
    
    private static void Main(string[] args)
    {
        RoomManager.Instance.Add();
        
        // DNS (Domain Name System) ex) www.naver.com -> 123.123.124.12
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress ipAddress = ipHost.AddressList[4];
        // IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);
        _listener.Init(endPoint, () => SessionManager.Instance.Generate());
        Console.WriteLine($"Listening... {endPoint}");

        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(new IPEndPoint(ipHost.AddressList[i], 7777));
        }

        JobTimer.Instance.Push(FlushRoom);
        
        while (true)
        {
            JobTimer.Instance.Flush();
        }
    }
}
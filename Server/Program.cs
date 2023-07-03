using System.Net;
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
        RoomManager.Instance.Add(1);
        
        // DNS (Domain Name System) ex) www.naver.com -> 123.123.124.12
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress? ipAddress = null;
        foreach (var ip in ipHost.AddressList)
        {
            if (ip.ToString().Contains("172")) ipAddress = ip;
        }
        // IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        if (ipAddress != null)
        {
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);
            _listener.Init(endPoint, () => SessionManager.Instance.Generate());
            Console.WriteLine($"Listening... {endPoint}");
        }

        JobTimer.Instance.Push(FlushRoom);
        
        while (true)
        {
            JobTimer.Instance.Flush();
        }
    }
}
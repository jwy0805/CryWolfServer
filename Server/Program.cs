using System.Net;
using System.Timers;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;
using Timer = System.Timers.Timer;

namespace Server;

public class Program
{
    private static Listener _listener = new Listener();

    private static void GameLogicTask()
    {
        while (true)
        {
            GameLogic.Instance.Update();
            Thread.Sleep(10);
        }
    }

    private static void NetworkTask()
    {
        while (true)
        {
            List<ClientSession> sessions = SessionManager.Instance.GetSessions();
            foreach (var session in sessions)
            {
                session.FlushSend();
            }
            
            Thread.Sleep(10);
        }
    }

    private static void StartServerInfoTask()
    {
        var t = new Timer();
        t.AutoReset = true;
        t.Elapsed += new ElapsedEventHandler((s, e) =>
        {
            // using SharedDbContext shared = new SharedDbContext();
        });
        t.Interval = 10 * 1000;
        t.Start();
    }
    
    private static void DbTask()
    {
        while (true)
        {
            DbTransaction.Instance.Flush();
            Thread.Sleep(10);
        }
    }
    
    public static string Name { get; set; } = "Server1";
    public static int Port { get; set; } = 7777;
    public static string IpAddress { get; set; }
    
    private static void Main(string[] args)
    {
        DataManager.LoadData();
        // GameLogic.Instance.Push(() => { GameLogic.Instance.Add(1);});
        
        // DNS (Domain Name System) ex) www.naver.com -> 123.123.124.12
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        // foreach (var address in ipHost.AddressList) Console.WriteLine($"{address}");
        // IPAddress? ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("192."));
        IPAddress? ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("172."));
        Console.WriteLine(ipAddress);
        // IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        if (ipAddress != null)
        {
            IPEndPoint endPoint = new IPEndPoint(ipAddress, Port);
            _listener.Init(endPoint, () => SessionManager.Instance.Generate());
            Console.WriteLine($"Listening... {endPoint}");
        }
        
        // StartServerInfoTask();
        
        Task gameLogicTask = new Task(GameLogicTask, TaskCreationOptions.LongRunning);
        gameLogicTask.Start();
        
        Task networkTask = new Task(NetworkTask, TaskCreationOptions.LongRunning);
        networkTask.Start();
        
        DbTask();
    }
}
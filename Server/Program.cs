using System.Net;
using System.Numerics;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Util;
using ServerCore;
using SharedDB;

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
        var t = new System.Timers.Timer();
        t.AutoReset = true;
        t.Elapsed += new System.Timers.ElapsedEventHandler((s, e) =>
        {
            using SharedDbContext shared = new SharedDbContext();
            var serverDb = shared.Servers.FirstOrDefault(server => server.Name == Name);
            if (serverDb != null)
            {
                serverDb.IpAddress = IpAddress;
                serverDb.Port = Port;
                serverDb.BusyScore = SessionManager.Instance.GetBusyScore();
                shared.SaveChangesExtended();
            }
            else
            {
                serverDb = new ServerDb
                {
                    Name = Program.Name,
                    IpAddress = Program.IpAddress,
                    Port = Program.Port,
                    BusyScore = SessionManager.Instance.GetBusyScore()
                };
                shared.Servers.Add(serverDb);
                shared.SaveChangesExtended();
            }
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
        ConfigManager.LoadConfig();
        DataManager.LoadData();
        GameLogic.Instance.Push(() => { GameLogic.Instance.Add(1);});
        
        // DNS (Domain Name System) ex) www.naver.com -> 123.123.124.12
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        // foreach (var address in ipHost.AddressList) Console.WriteLine($"{address}");
        // IPAddress? ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("192."));
        IPAddress? ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("172."));
        // IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        if (ipAddress != null)
        {
            IPEndPoint endPoint = new IPEndPoint(ipAddress, Port);
            _listener.Init(endPoint, () => SessionManager.Instance.Generate());
            Console.WriteLine($"Listening... {endPoint}");
        }
        
        StartServerInfoTask();
        
        Task gameLogicTask = new Task(GameLogicTask, TaskCreationOptions.LongRunning);
        gameLogicTask.Start();
        
        Task networkTask = new Task(NetworkTask, TaskCreationOptions.LongRunning);
        networkTask.Start();
        
        DbTask();
    }
}
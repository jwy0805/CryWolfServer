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
    private static int _environment = 0; // 0: local, 1: docker
    public static int Port { get; set; } = 7777;

    private static void Main(string[] args)
    {
        DataManager.LoadData();
        // GameLogic.Instance.Push(() => { GameLogic.Instance.Add(1); });

        // DNS
        var host = Dns.GetHostName();
        var ipHost = Dns.GetHostEntry(host);
        IPAddress? ipAddress;
        switch (_environment)
        {
            case 1:
                var ipStr = Environment.GetEnvironmentVariable("SERVER_IP");
                if (ipStr != null && IPAddress.TryParse(ipStr, out var ipAdr)) ipAddress = ipAdr;
                else ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("172."));
                break;
            default:
                ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("172."));
                break;
        }

        Console.WriteLine(ipAddress);

        if (ipAddress != null)
        {
            var endPoint = new IPEndPoint(ipAddress, Port);
            _listener.Init(endPoint, () =>
            {
                Console.WriteLine("listener callback");
                var session = SessionManager.Instance.Generate();
                return session;
            });
            Console.WriteLine($"Listening... {endPoint}");
        }

        var gameLogicTask = new Task(GameLogicTask, TaskCreationOptions.LongRunning);
        gameLogicTask.Start();

        var networkTask = new Task(NetworkTask, TaskCreationOptions.LongRunning);
        networkTask.Start();

        DbTask();
    }
        
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
}
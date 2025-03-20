using System.Net;
using System.Text;
using System.Timers;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;
using Timer = System.Timers.Timer;
// ReSharper disable FunctionNeverReturns

namespace Server;

public static class Program
{
    private static readonly Listener Listener = new();
    private static readonly int Port = 7777;

    private static void Main(string[] args)
    {
        DataManager.LoadData();

        // DNS
        IPAddress? ipAddress;
        if (NetworkManager.Instance.Environment == Env.Local)
        {
            var host = Dns.GetHostName();
            var ipHost = Dns.GetHostEntry(host);

            foreach (var ip in ipHost.AddressList)
            {
                Console.WriteLine(ip);
            }
            
            ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("192."));

            if (ipAddress == null)
            {
                Console.WriteLine("Failed to find a local IP address. Check your configuration.");
                return;
            }
        }
        else
        {
            Console.WriteLine("Environment: " + NetworkManager.Instance.Environment);
            const string host = "crywolf-socket";
            var ipHost = Dns.GetHostEntry(host);
            foreach (var address in ipHost.AddressList)
            {
                Console.WriteLine($"Address: {address}");
            }
            ipAddress = ipHost.AddressList.FirstOrDefault();

            if (ipAddress == null)
            {
                Console.WriteLine($"Failed to resolve DNS for host: {host}");
                return;
            }
        }
        
        var endPoint = new IPEndPoint(ipAddress, Port);
        Listener.Init(endPoint, () => SessionManager.Instance.Generate());
        Console.WriteLine($"Listening... {endPoint}");
        
        NetworkManager.Instance.StartHttpServer();

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

    // Only for test
    public static void GameLogicTaskForTest()
    {
        
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
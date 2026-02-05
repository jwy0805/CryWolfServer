using System.Diagnostics;
using System.Net;
using System.Text;
using System.Timers;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Util;
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
            IPHostEntry ipHost;
            try
            {
                ipHost = Dns.GetHostEntry(host);
            }
            catch (Exception)
            {
                ipHost = Dns.GetHostEntry("127.0.0.1");
            }

            foreach (var ip in ipHost.AddressList)
            {
                Console.WriteLine(ip);
            }
            
            ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("172."));
            
            if (ipAddress == null)
            {
                ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            }

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
        
        var metricTask = new Task(MetricsTask, TaskCreationOptions.LongRunning);
        metricTask.Start();
        
        Task.WaitAll(gameLogicTask, networkTask, metricTask);
    }
    
    private static void GameLogicTask()
    {
        var stopwatch = new Stopwatch();
        while (true)
        {
            stopwatch.Restart();
            try
            {
                GameLogic.Instance.Update();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[GameLogicTask] Fatal Error: {e}");
            }
            stopwatch.Stop();
            Metrics.RecordLoopMs(stopwatch.Elapsed.TotalMilliseconds);
            
            Thread.Sleep(10);
        }
    }

    private static void NetworkTask()
    {
        while (true)
        {
            try
            {
                List<ClientSession> sessions = SessionManager.Instance.GetSessions();
                foreach (var session in sessions)
                {
                    try
                    {
                        session.FlushSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[NetworkTask] Session {session.SessionId} Send Error: {e.Message}");
                        session.Disconnect();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[NetworkTask] Fatal Error: {e}");
            }
            
            Thread.Sleep(10);
        }
    }
    
    private static void MetricsTask()
    {
        var metricDir = Environment.GetEnvironmentVariable("METRIC_LOG_DIR");
        var localDir = "./logs";
        var reporter = new MetricsReporter(metricDir ?? localDir, 15000);
        reporter.Run();
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
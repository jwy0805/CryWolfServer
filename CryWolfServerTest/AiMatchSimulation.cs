using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using CryWolfServerTest.TestClient;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using Server.Game;
using Server.Util;
using ServerCore;

namespace CryWolfServerTest;

[TestFixture]
public class AiMatchSimulation
{
    private static readonly Env Env = Env.Prod;
    private static readonly HttpClient HttpClient = new(new SocketsHttpHandler { MaxConnectionsPerServer = 1024 });
    
    [OneTimeSetUp]
    public void SetUp()
    {
        
    }

    [Test]
    public async Task EnqueueAiMatches()
    {
        const int aiCount = 500;
        const int firstBatchCount = 400;
        Dictionary<int, TestSession> aiSessions = new();
        List<Task> sessionTasks1 = new();
        List<Task> enqueueTasks1 = new();

        for (var aiId = 1; aiId <= firstBatchCount; aiId++)
        {
            sessionTasks1.Add(ConnectGameSession(aiId, aiSessions));
        }

        await Task.WhenAll(sessionTasks1);
        await Task.Delay(TimeSpan.FromSeconds(5));

        var halfCount = firstBatchCount / 2;
        var factions = Enumerable.Repeat(Faction.Sheep, halfCount)
            .Concat(Enumerable.Repeat(Faction.Wolf, firstBatchCount - halfCount))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
        
        for (var aiId = 1; aiId <= firstBatchCount; aiId++)
        {
            var faction = factions[aiId - 1];
            enqueueTasks1.Add(Enqueue(aiSessions[aiId].SessionId, faction));
        }
        
        await Task.WhenAll(enqueueTasks1);
        await Task.Delay(TimeSpan.FromSeconds(75));
        
        List<Task> sessionTasks2 = new();
        List<Task> enqueueTasks2 = new();
        
        for (var aiId = firstBatchCount + 1; aiId <= aiCount; aiId++)
        {
            sessionTasks2.Add(ConnectGameSession(aiId, aiSessions));
        }
        
        await Task.WhenAll(sessionTasks2);
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        var halfCount2 = (aiCount - firstBatchCount) / 2;
        var factions2 = Enumerable.Repeat(Faction.Sheep, halfCount2)
            .Concat(Enumerable.Repeat(Faction.Wolf, aiCount - firstBatchCount - halfCount2))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
        
        for (var aiId = firstBatchCount + 1; aiId <= aiCount; aiId++)
        {
            var faction = factions2[aiId - firstBatchCount - 1];
            enqueueTasks2.Add(Enqueue(aiSessions[aiId].SessionId, faction));
        }
        
        await Task.WhenAll(enqueueTasks2);
        await Task.Delay(TimeSpan.FromSeconds(400));
    }

    private static async Task ConnectGameSession(int aiId, Dictionary<int, TestSession> aiSessions)
    {
        var port = SetPort(Env);
        var ipAddress = await SetIpAddress(Env);
        var endPoint = new IPEndPoint(ipAddress, port);
        
        aiSessions[aiId] = new TestSession();
        
        new Connector().Connect(endPoint, () => aiSessions[aiId]);
    }
    
    private static async Task<bool> Enqueue(int sessionId, Faction faction)
    {
        var url = SetUrl(Env);
        var requestUrl = $"{url}/api/Match/EnqueueAiMatch";
        var matchRequest = new EnqueueAiMatchPacketRequired
        {
            SessionId = sessionId,
            Faction = faction
        };
        
        var json = JsonConvert.SerializeObject(matchRequest);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        try
        {
            await HttpClient.PostAsync(requestUrl, content);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Enqueue error: {e}");
        }
        
        return false;
    }

    private static async Task<IPAddress> SetIpAddress(Env env)
    {
        string host;
        IPHostEntry ipHost;
        IPAddress? ipAddress;
        
        if (env == Env.Local)
        {
            host = Dns.GetHostName();
            ipHost = await Dns.GetHostEntryAsync(host);
            ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.ToString().Contains("172."));   
        }
        else
        {
            host = "crywolf-tcpbalancer-5dadfff82e2ee15a.elb.ap-northeast-2.amazonaws.com";
            ipHost = await Dns.GetHostEntryAsync(host);
            ipAddress = ipHost.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
        
        if (ipAddress == null)
        {
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        return ipAddress;
    }
    
    private static int SetPort(Env env)
    {
        return env switch
        {
            Env.Local => 7777,
            _ => 7780
        };
    }

    private static string SetUrl(Env env)
    {
        return env switch
        {
            Env.Local => "https://localhost:7270",
            _ => "https://hamonstudio.net"
        };
    }
}
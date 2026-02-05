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
    
    [OneTimeSetUp]
    public void SetUp()
    {
        
    }

    [Test]
    public async Task EnqueueAiMatches()
    {
        const int aiCount = 1000;
        Dictionary<int, TestSession> aiSessions = new();
        List<Task> sessionTasks = new();
        List<Task> enqueueTasks = new();

        for (var aiId = 1; aiId <= aiCount; aiId++)
        {
            sessionTasks.Add(ConnectGameSession(aiId, aiSessions));
        }

        await Task.WhenAll(sessionTasks);
        await Task.Delay(TimeSpan.FromSeconds(5));

        var halfCount = aiCount / 2;
        var factions = Enumerable.Repeat(Faction.Sheep, halfCount)
            .Concat(Enumerable.Repeat(Faction.Wolf, aiCount - halfCount))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
        
        for (var aiId = 1; aiId <= aiCount; aiId++)
        {
            var faction = factions[aiId - 1];
            enqueueTasks.Add(Enqueue(aiSessions[aiId].SessionId, faction));
        }
        
        await Task.WhenAll(enqueueTasks);
        await Task.Delay(TimeSpan.FromMinutes(10));
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
        using var httpClient = new HttpClient();
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
            await httpClient.PostAsync(requestUrl, content);
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
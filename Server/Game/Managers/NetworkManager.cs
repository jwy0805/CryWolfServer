using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;

namespace Server.Game;

public class NetworkManager
{
    private HttpListener? _httpListener;
    private readonly HttpClient _httpClient = new();
    private const int ApiPortLocal = 5281;
    
    public Env Environment => System.Environment.GetEnvironmentVariable("ENVIRONMENT") switch
    {
        "Local" => Env.Local,
        "Dev" => Env.Dev,
        "Stage" => Env.Stage,
        "Prod" => Env.Prod,
        _ => Env.Local
    };
    
    private string BaseUrl => Environment switch
    {
        Env.Local => $"http://localhost:{ApiPortLocal}/api",
        Env.Dev => "http://crywolf-api/api",
        Env.Stage => "http://crywolf-api/api",
        Env.Prod => "http://crywolf-api/api",
        _ => throw new ArgumentOutOfRangeException()
    };

    public static NetworkManager Instance { get; } = new();

    public void StartHttpServer()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://*:8081/");
        _httpListener.Start();
        Console.WriteLine("HTTP Server Started at 8081");
        Task.Run(HandleHttpRequests);
    }
    
    private async Task HandleHttpRequests()
    {
        if (_httpListener == null) return;
        while (_httpListener.IsListening)
        {
            var context = await _httpListener.GetContextAsync();
            var request = context.Request; 
            var response = context.Response;

            try
            {
                string responseString;

                switch (request.Url?.AbsolutePath)
                {
                    case "/match":
                        responseString = await HandleMatchRequest(request);
                        break;
                    case "/surrender":
                        responseString = await HandleSurrenderRequest(request);
                        break;
                    case "/test":
                        responseString = await HandleTestRequest(request);
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        response.Close();
                        continue;
                }
                
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.StatusCode = (int)HttpStatusCode.OK;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Http Error: {e}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                response.Close();
            }
        }
    }

    private async Task<string> HandleMatchRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
        {
            throw new HttpRequestException("Method Not Allowed", null, HttpStatusCode.MethodNotAllowed);
        }
        
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var requestBody = await reader.ReadToEndAsync();
        var matchRequest = JsonConvert.DeserializeObject<MatchSuccessPacketRequired>(requestBody);
        if (matchRequest == null)
        {
            throw new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest);
        }

        var task = await StartGameAsync(matchRequest);
        var matchResponse = new MatchSuccessPacketResponse { IsSuccess = task }; 
        
        return JsonConvert.SerializeObject(matchResponse);
    }
    
    private async Task<string> HandleSurrenderRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
        {
            throw new HttpRequestException("Method Not Allowed", null, HttpStatusCode.MethodNotAllowed);
        }
        
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var requestBody = await reader.ReadToEndAsync();
        var surrenderRequest = JsonConvert.DeserializeObject<GameResultPacketRequired>(requestBody);
        if (surrenderRequest == null)
        {
            throw new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest);
        }

        var task = await SurrenderGameAsync(surrenderRequest);
        var surrenderResponse = new GameResultPacketResponse { GetGameResultOk = task };
        
        return JsonConvert.SerializeObject(surrenderResponse);
    }
    
    private async Task<string> HandleTestRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
        {
            throw new HttpRequestException("Method Not Allowed", null, HttpStatusCode.MethodNotAllowed);
        }
        
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var requestBody = await reader.ReadToEndAsync();
        var testRequest = JsonConvert.DeserializeObject<TestApiToSocketRequired>(requestBody);
        if (testRequest == null)
        {
            throw new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest);
        }
        
        var testResponse = new TestApiToSocketResponse { TestOk = testRequest.Test };
        
        return JsonConvert.SerializeObject(testResponse);
    }

    # region StartGame

    private async Task<bool> StartGameAsync(MatchSuccessPacketRequired packet, DateTime? startTime = null)
    {
        startTime ??= DateTime.UtcNow;
        var tcs = new TaskCompletionSource<bool>();
        
        GameLogic.Instance.Push(() =>
        {
            if (packet.WolfUserId == packet.SheepUserId)
            {
                StartTestGame(packet);
            }
            else
            {
                var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
                var sheepPlayer = CreatePlayer(room, packet, Faction.Sheep);
                var wolfPlayer = CreatePlayer(room, packet, Faction.Wolf);
        
                if (sheepPlayer.Session == null || wolfPlayer.Session == null)
                {
                    if ((DateTime.UtcNow - startTime.Value).TotalMilliseconds > 5000)
                    {
                        Console.WriteLine("Session timeout.");
                        tcs.SetResult(false);
                        return;
                    }
        
                    Console.WriteLine("Session is not ready yet.");
                    GameLogic.Instance.PushAfter(400, () => _ = RetryStartGameAsync(packet, startTime, tcs));
                    return;
                }
        
                SendStartGamePacket(sheepPlayer, wolfPlayer, packet);
            }
            
            var sendPacket = new SendMatchInfoPacketRequired
            {
                SheepUserId = packet.SheepUserId,
                SheepSessionId = packet.SheepSessionId,
                WolfUserId = packet.WolfUserId,
                WolfSessionId = packet.WolfSessionId,
            };
        
#pragma warning disable CS4014 // 이 호출을 대기하지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다.
            SendRequestToApiAsync<SendMatchInfoPacketResponse>("Match/SetMatchInfo", sendPacket, HttpMethod.Post);
#pragma warning restore CS4014 // 이 호출을 대기하지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다.
            tcs.SetResult(true);
        });
        
        return await tcs.Task;
    }
    
    private Player CreatePlayer(GameRoom room, MatchSuccessPacketRequired required, Faction faction)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };
        
        player.Room = room;
        player.Faction = faction;
        player.Info.Name = faction == Faction.Sheep ? $"Player_{required.SheepUserName}" : $"Player_{required.WolfUserName}";
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.CharacterId = faction == Faction.Sheep ? required.SheepCharacterId : required.WolfCharacterId;
        player.AssetId = faction == Faction.Sheep ? (int)required.SheepId : (int)required.EnchantId;
        player.WinRankPoint = faction == Faction.Sheep ? required.WinPointSheep : required.WinPointWolf;
        player.LoseRankPoint = faction == Faction.Sheep ? required.LosePointSheep : required.LosePointWolf;
        player.RankPoint = faction == Faction.Sheep ? required.SheepRankPoint : required.WolfRankPoint;
        player.Session = faction == Faction.Sheep 
            ? SessionManager.Instance.Find(required.SheepSessionId)
            : SessionManager.Instance.Find(required.WolfSessionId);

        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        player.Session.MyPlayer = player;
        player.Session.UserId = faction == Faction.Sheep ? required.SheepUserId : required.WolfUserId;

        return player;
    }

    private Player CreateNpc(Player player, MatchSuccessPacketRequired required, int sheepId = 901, int enchantId = 1001)
    {
        // This is a test NPC, so this has to be changed later when the single play mode is implemented.
        var npc = ObjectManager.Instance.Add<Player>();
        var faction = player.Faction == Faction.Sheep ? Faction.Wolf : Faction.Sheep;
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };

        npc.Faction = faction;
        npc.Info.Name = $"NPC_{player.Info.Name}";
        npc.PosInfo = position;
        npc.Info.PosInfo = position;
        npc.CharacterId = faction == Faction.Sheep ? required.SheepCharacterId : required.WolfCharacterId;
        npc.AssetId = faction == Faction.Sheep ? sheepId : enchantId;

        return npc;
    }

    private void SendStartGamePacket(Player sheepPlayer, Player wolfPlayer, MatchSuccessPacketRequired packet)
    {
        var (matchPacketForSheep, matchPacketForWolf) = MakeMatchPacket(packet);
        sheepPlayer.Session?.Send(matchPacketForSheep);
        wolfPlayer.Session?.Send(matchPacketForWolf);
    }

    private async void StartTestGame(MatchSuccessPacketRequired required)
    {
        var faction = required.SheepUserName == "Test" ? Faction.Wolf : Faction.Sheep;
        var room = GameLogic.Instance.CreateGameRoom(required.MapId);
        var player = CreatePlayer(room, required, faction);
        var npc = CreateNpc(player, required);
        var matchPacket = new S_MatchMakingSuccess
        {
            EnemyUserName = npc.Info.Name,
            EnemyRankPoint = required.SheepRankPoint,
            EnemyCharacterId = (int)required.SheepCharacterId,
            EnemyAssetId = player.Faction == Faction.Sheep ? (int)required.EnchantId : (int)required.SheepId,
        };

        room.Npc = npc;
        
        foreach (var unitId in required.SheepUnitIds)
        {
            matchPacket.EnemyUnitIds.Add((int)unitId);
        }

        player.Session?.Send(matchPacket);

        await Task.Delay(6000);
        room.RoomActivated = true;
    }

    private Tuple<S_MatchMakingSuccess, S_MatchMakingSuccess> MakeMatchPacket(MatchSuccessPacketRequired packet)
    {
        var matchPacketForSheep = new S_MatchMakingSuccess
        {
            EnemyUserName = packet.WolfUserName,
            EnemyRankPoint = packet.WolfRankPoint,
            EnemyCharacterId = (int)packet.WolfCharacterId,
            EnemyAssetId = (int)packet.EnchantId,
        };

        foreach (var unitId in packet.WolfUnitIds)
        {
            matchPacketForSheep.EnemyUnitIds.Add((int)unitId);
        }
        
        foreach (var achievement in packet.WolfAchievements)
        {
            matchPacketForSheep.EnemyAchievements.Add(achievement);
        }
        
        var matchPacketForWolf = new S_MatchMakingSuccess
        {
            EnemyUserName = packet.SheepUserName,
            EnemyRankPoint = packet.SheepRankPoint,
            EnemyCharacterId = (int)packet.SheepCharacterId,
            EnemyAssetId = (int)packet.SheepId,
        };
        
        foreach (var unitId in packet.SheepUnitIds)
        {
            matchPacketForWolf.EnemyUnitIds.Add((int)unitId);
        }
        
        foreach (var achievement in packet.SheepAchievements)
        {
            matchPacketForWolf.EnemyAchievements.Add(achievement);
        }

        return new Tuple<S_MatchMakingSuccess, S_MatchMakingSuccess>(matchPacketForSheep, matchPacketForWolf);
    }
    
    private async Task RetryStartGameAsync(MatchSuccessPacketRequired packet, DateTime? startTime,
        TaskCompletionSource<bool> tcs)
    {
        var result = await StartGameAsync(packet, startTime);
        tcs.TrySetResult(result);
    }

    #endregion

    #region Surrender

    private async Task<bool> SurrenderGameAsync(GameResultPacketRequired packet)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.FindByUserId(packet.UserId);
            if (room == null)
            {
                Console.WriteLine("Room not found.");
                tcs.SetResult(false);
            }
            else
            {
                var winnerId = room.FindPlayer(go =>
                    go is Player player && player.Session?.UserId != packet.UserId)?.Session?.UserId ?? -1;
                room.GameOver(winnerId, packet.UserId);
                tcs.SetResult(true);
            }
        });

        return await tcs.Task;
    }

    #endregion
    
    public async Task OnSessionDisconnected(int sessionId)
    {
        var session = SessionManager.Instance.Find(sessionId);
        if (session == null) return;
        
        var packet = new SessionDisconnectPacketRequired
        {
            UserId = session.UserId,
            SessionId = sessionId
        };
        
        await SendRequestToApiAsync<SessionDisconnectPacketResponse>("Match/SessionDisconnect", packet, HttpMethod.Post);
    }
    
    public async Task<T?> SendRequestToApiAsync<T>(string url, object? obj, HttpMethod method)
    {
        var sendUrl = $"{BaseUrl}/{url}";
        byte[]? jsonBytes = null;
        if (obj != null)
        {
            var jsonStr = JsonConvert.SerializeObject(obj);
            jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
        }
        
        var request = new HttpRequestMessage(method, sendUrl)
        {
            Content = new ByteArrayContent(jsonBytes ?? Array.Empty<byte>())
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode == false)
        {
            throw new Exception($"Error: {response.StatusCode} : {response.ReasonPhrase}");
        }
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(responseJson);
    }
}
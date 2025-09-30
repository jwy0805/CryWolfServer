using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using Server.Data;

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
                    case "/friendlyMatch":
                        responseString = await HandleFriendlyMatchRequest(request);
                        break;
                    case "/surrender":
                        responseString = await HandleSurrenderRequest(request);
                        break;
                    case "/singlePlay":
                        responseString = await HandleSingleGameRequest(request);
                        break;
                    case "/tutorial":
                        responseString = await HandleTutorialRequest(request);
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
                await response.OutputStream.WriteAsync(buffer);
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

    private async Task<string> HandleFriendlyMatchRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
        {
            throw new HttpRequestException("Method Not Allowed", null, HttpStatusCode.MethodNotAllowed);
        }
        
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var requestBody = await reader.ReadToEndAsync();
        var matchRequest = JsonConvert.DeserializeObject<FriendlyMatchPacketRequired>(requestBody);
        if (matchRequest == null)
        {
            throw new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest);
        }

        var task = await StartFriendlyGameAsync(matchRequest);
        var matchResponse = new FriendlyMatchPacketResponse { IsSuccess = task };
        
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

    private async Task<string> HandleTutorialRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
        {
            throw new HttpRequestException("Method Not Allowed", null, HttpStatusCode.MethodNotAllowed);
        }
        
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var requestBody = await reader.ReadToEndAsync();
        var tutorialRequest = JsonConvert.DeserializeObject<TutorialStartPacketRequired>(requestBody);
        if (tutorialRequest == null)
        {
            throw new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest);
        }
        
        var task = await StartTutorialAsync(tutorialRequest);
        var response = new TutorialStartPacketResponse { TutorialStartOk = task };

        return JsonConvert.SerializeObject(response);
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

    private async Task<string> HandleSingleGameRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
        {
            throw new HttpRequestException("Method Not Allowed", null, HttpStatusCode.MethodNotAllowed);
        }
        
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var requestBody = await reader.ReadToEndAsync();
        var singleGameRequest = JsonConvert.DeserializeObject<SinglePlayStartPacketRequired>(requestBody);
        if (singleGameRequest == null)
        {
            throw new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest);
        }
        
        var task = await StartSingleGameAsync(singleGameRequest);
        var response = new SinglePlayStartPacketResponse { SinglePlayStartOk = task };
        
        return JsonConvert.SerializeObject(response);
    }
    
    # region StartGame

    private async Task<bool> StartGameAsync(MatchSuccessPacketRequired packet, DateTime? startTime = null)
    {
        startTime ??= DateTime.UtcNow;
        var tcs = new TaskCompletionSource<bool>();

        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            if (packet.IsTestGame)
            {
                var faction = packet.SheepUserName == "Test" ? Faction.Wolf : Faction.Sheep;
                var player = CreatePlayer(room, packet, faction);
                var npcCharacterId = faction == Faction.Sheep ? packet.WolfCharacterId : packet.SheepCharacterId;
                var npcAssetId = faction == Faction.Sheep ? (int)packet.EnchantId : (int)packet.SheepId;
                var npc = CreateNpc(room, player, npcCharacterId, npcAssetId);
                var matchPacket = new S_MatchMakingSuccess
                {
                    EnemyUserName = npc.Info.Name,
                    EnemyRankPoint = packet.SheepRankPoint,
                    EnemyCharacterId = (int)packet.SheepCharacterId,
                    EnemyAssetId = player.Faction == Faction.Sheep ? (int)packet.EnchantId : (int)packet.SheepId,
                };
        
                room.Npc = npc;
                room.GameMode = GameMode.Test;
        
                foreach (var unitId in packet.SheepUnitIds)
                {
                    matchPacket.EnemyUnitIds.Add((int)unitId);
                }

                player.Session?.Send(matchPacket);
            }
            else
            {
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
        
                room.GameMode = GameMode.Rank;
                SendStartGamePacket(sheepPlayer, wolfPlayer, packet);
            }
            
            var sendPacket = new SendMatchInfoPacketRequired
            {
                SheepUserId = packet.SheepUserId,
                SheepSessionId = packet.SheepSessionId,
                WolfUserId = packet.WolfUserId,
                WolfSessionId = packet.WolfSessionId,
            };
        
            var requestTask = SendRequestToApiAsync<SendMatchInfoPacketResponse>(
                "Match/SetMatchInfo", sendPacket, HttpMethod.Post);
            var timeTask = Task.Delay(6000);
            var tasks = Task.WhenAll(requestTask, timeTask);
            tasks.ContinueWith(_ =>
            {
                if (requestTask.Result is { SendMatchInfoOk: true })
                {
                    room.RoomActivated = true;
                    Console.WriteLine("Start Game Async - room activated");
                    tcs.SetResult(true);
                }
                else
                {
                    Console.WriteLine("Start Game Async - room not activated");
                    tcs.SetResult(false);
                }
            });
        });
        
        return await tcs.Task;
    }

    private async Task<bool> StartFriendlyGameAsync(FriendlyMatchPacketRequired packet, DateTime? startTime = null)
    {
        Console.WriteLine("Start Friendly Game Async");
        startTime ??= DateTime.UtcNow;
        var tcs = new TaskCompletionSource<bool>();
        
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            var sheepPlayer = CreatePlayerFriendly(room, packet, Faction.Sheep);
            var wolfPlayer = CreatePlayerFriendly(room, packet, Faction.Wolf);
            
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
            
            room.GameMode = GameMode.Friendly;
            var sendPacket = new SendMatchInfoPacketRequired
            {
                SheepUserId = packet.SheepUserId,
                SheepSessionId = packet.SheepSessionId,
                WolfUserId = packet.WolfUserId,
                WolfSessionId = packet.WolfSessionId,
            };
        
            var requestTask = SendRequestToApiAsync<SendMatchInfoPacketResponse>(
                "Match/SetMatchInfo", sendPacket, HttpMethod.Post);
            var timeTask = Task.Delay(6000);
            var tasks = Task.WhenAll(requestTask, timeTask);
            tasks.ContinueWith(_ =>
            {
                if (requestTask.Result is { SendMatchInfoOk: true })
                {
                    room.RoomActivated = true;
                    Console.WriteLine("Start Game Async - room activated");
                    tcs.SetResult(true);
                }
                else
                {
                    Console.WriteLine("Start Game Async - room not activated");
                    tcs.SetResult(false);
                }
            });
        });

        return await tcs.Task;
    }
    
    private async Task<bool> StartSingleGameAsync(SinglePlayStartPacketRequired packet)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            var player = CreatePlayerSingle(room, packet);
            room.Npc = CreateNpc(room, player, (CharacterId)packet.EnemyCharacterId, packet.EnemyAssetId, packet.EnemyUnitIds);
            room.GameMode = GameMode.Single;
            room.StageId = packet.StageId;
            room.RoomActivated = true;
            tcs.SetResult(true);
        });
        
        return await tcs.Task;
    }

    private async Task<bool> StartTutorialAsync(TutorialStartPacketRequired packet)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            var player = CreatePlayerTutorial(room, packet);
            room.Npc = CreateNpc(room, player, (CharacterId)packet.EnemyCharacterId, packet.EnemyAssetId);
            room.GameMode = GameMode.Tutorial;
            room.RoomActivated = true;
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
        var sheepCharacterName = required.SheepCharacterId.ToString();
        var wolfCharacterName = required.WolfCharacterId.ToString();

        player.Room = room;
        player.Faction = faction;
        player.Info.Name = faction == Faction.Sheep ? sheepCharacterName : wolfCharacterName;
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.CharacterId = faction == Faction.Sheep ? required.SheepCharacterId : required.WolfCharacterId;
        player.AssetId = faction == Faction.Sheep ? (int)required.SheepId : (int)required.EnchantId;
        player.WinRankPoint = faction == Faction.Sheep ? required.WinPointSheep : required.WinPointWolf;
        player.LoseRankPoint = faction == Faction.Sheep ? required.LosePointSheep : required.LosePointWolf;
        player.RankPoint = faction == Faction.Sheep ? required.SheepRankPoint : required.WolfRankPoint;
        player.UnitIds = faction == Faction.Sheep ? required.SheepUnitIds : required.WolfUnitIds;
        player.Session = faction == Faction.Sheep 
            ? SessionManager.Instance.Find(required.SheepSessionId)
            : SessionManager.Instance.Find(required.WolfSessionId);

        Console.WriteLine($"Create Player -> {room.RoomId} {required.SheepSessionId} : {required.WolfSessionId}" );
        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        player.Session.MyPlayer = player;
        player.Session.UserId = faction == Faction.Sheep ? required.SheepUserId : required.WolfUserId;

        return player;
    }

    private Player CreatePlayerFriendly(GameRoom room, FriendlyMatchPacketRequired required, Faction faction)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };
        var sheepCharacterName = required.SheepCharacterId.ToString();
        var wolfCharacterName = required.WolfCharacterId.ToString();

        player.Room = room;
        player.Faction = faction;
        player.Info.Name = faction == Faction.Sheep ? sheepCharacterName : wolfCharacterName;
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.CharacterId = faction == Faction.Sheep ? required.SheepCharacterId : required.WolfCharacterId;
        player.AssetId = faction == Faction.Sheep ? (int)required.SheepId : (int)required.EnchantId;
        player.UnitIds = faction == Faction.Sheep ? required.SheepUnitIds : required.WolfUnitIds;
        player.Session = faction == Faction.Sheep 
            ? SessionManager.Instance.Find(required.SheepSessionId)
            : SessionManager.Instance.Find(required.WolfSessionId);
        
        Console.WriteLine($"Create Player -> {room.RoomId} {required.SheepSessionId} : {required.WolfSessionId}" );
        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        player.Session.MyPlayer = player;
        player.Session.UserId = faction == Faction.Sheep ? required.SheepUserId : required.WolfUserId;

        return player;
    }
    
    private Player CreatePlayerSingle(GameRoom room, SinglePlayStartPacketRequired required)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var faction = required.UserFaction;
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };
        
        player.Room = room;
        player.Faction = faction;
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.Info.Name = ((CharacterId)required.CharacterId).ToString();
        player.CharacterId = (CharacterId)required.CharacterId;
        player.AssetId = required.AssetId;
        player.UnitIds = required.UnitIds;
        player.Session = SessionManager.Instance.Find(required.SessionId);

        Console.WriteLine($"{required.SessionId} single play, room {room.RoomId}");
        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        player.Session.MyPlayer = player;
        player.Session.UserId = required.UserId;

        return player;
    }

    private Player CreatePlayerTutorial(GameRoom room, TutorialStartPacketRequired required)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var faction = required.UserFaction;
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };

        player.Room = room;
        player.Faction = faction;
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.Info.Name = ((CharacterId)required.CharacterId).ToString();
        player.CharacterId = (CharacterId)required.CharacterId;
        player.AssetId = required.AssetId;
        player.UnitIds = required.UnitIds;
        player.Session = SessionManager.Instance.Find(required.SessionId);

        Console.WriteLine($"{required.SessionId} in tutorial");
        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {player.Session?.UserId}");
            return player;
        }
        
        player.Session.MyPlayer = player;
        player.Session.UserId = required.UserId;

        return player;
    }

    private Player CreateNpc(GameRoom room, Player player, CharacterId characterId, int assetId, UnitId[]? unitIds = null)
    {
        unitIds ??= Array.Empty<UnitId>();
        // This is a test NPC, so this has to be changed later when the single play mode is implemented.
        var npc = ObjectManager.Instance.Add<Player>();
        var faction = player.Faction == Faction.Sheep ? Faction.Wolf : Faction.Sheep;
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };

        npc.Room = room;
        npc.Faction = faction;
        npc.Info.Name = characterId.ToString();
        npc.PosInfo = position;
        npc.Info.PosInfo = position;
        npc.CharacterId = characterId;
        npc.AssetId = assetId;
        npc.UnitIds = unitIds;

        Console.WriteLine($"Create NPC -> {npc.Info.Name}");
        return npc;
    }

    private void SendStartGamePacket(Player sheepPlayer, Player wolfPlayer, MatchSuccessPacketRequired packet)
    {
        var (matchPacketForSheep, matchPacketForWolf) = MakeMatchPacket(packet);
        sheepPlayer.Session?.Send(matchPacketForSheep);
        wolfPlayer.Session?.Send(matchPacketForWolf);
    }

    private void StartTestGame(MatchSuccessPacketRequired required, GameRoom room)
    {
        
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
    
    private async Task RetryStartGameAsync(FriendlyMatchPacketRequired packet, DateTime? startTime,
        TaskCompletionSource<bool> tcs)
    {
        var result = await StartFriendlyGameAsync(packet, startTime);
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
                Console.WriteLine($"Room not found.");
                tcs.SetResult(false);
            }
            else
            {
                var winnerId = room.FindPlayer(go =>
                    go is Player player && player.Session?.UserId != packet.UserId)?.Session?.UserId ?? -1;
                _ = room.GameOver(winnerId, packet.UserId);
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
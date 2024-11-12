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
    private string BaseUrl => $"http://localhost:{ApiPortLocal}/api";

    public static NetworkManager Instance { get; } = new();

    public void StartHttpServer()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://*:8081/");
        _httpListener.Start();
        Console.WriteLine("HTTP Server Started");
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
                if (request.Url?.AbsolutePath != "/match")
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Close();
                    continue;
                }

                if (request.HttpMethod != "POST")
                {
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    response.Close();
                    continue;
                }
                
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var requestBody = await reader.ReadToEndAsync();
                
                var matchRequest = JsonConvert.DeserializeObject<MatchSuccessPacketRequired>(requestBody);
                if (matchRequest == null)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Close();
                    continue;
                }

                var task = StartGameAsync(matchRequest);
                await task;
                
                var matchResponse = new MatchSuccessPacketResponse { IsSuccess = task.Result };
                var responseString = JsonConvert.SerializeObject(matchResponse);
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
                var sheepPlayer = CreatePlayer(room, packet.SheepUserId, packet.SheepSessionId, Faction.Sheep);
                var wolfPlayer = CreatePlayer(room, packet.WolfUserId, packet.WolfSessionId, Faction.Wolf);

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
            tcs.SetResult(true);
        });

        return await tcs.Task;
    }

    private Player CreatePlayer(GameRoom room, int userId, int sessionId, Faction faction)
    {
        var player = ObjectManager.Instance.Add<Player>();
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };
        
        player.Room = room;
        player.Faction = faction;
        player.Info.Name = $"Player_{userId}";
        player.PosInfo = position;
        player.Info.PosInfo = position;
        player.Session = SessionManager.Instance.Find(sessionId);

        if (player.Session == null)
        {
            Console.WriteLine($"Session not found for user : {userId} / session : {sessionId}");
            return player;
        }
        
        player.Session.MyPlayer = player;
        player.Session.UserId = userId;

        return player;
    }

    private Player CreateNpc(Player player)
    {
        var npc = ObjectManager.Instance.Add<Player>();
        var faction = player.Faction == Faction.Sheep ? Faction.Wolf : Faction.Sheep;
        var position = faction == Faction.Sheep 
            ? new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = -22, Dir = 0 }
            : new PositionInfo { State = State.Idle, PosX = 0, PosY = 13.8f, PosZ = 22, Dir = 180 };

        npc.Faction = faction;
        npc.Info.Name = $"NPC_{player.Info.Name}";
        npc.PosInfo = position;
        npc.Info.PosInfo = position;

        return npc;
    }

    private void SendStartGamePacket(Player sheepPlayer, Player wolfPlayer, MatchSuccessPacketRequired packet)
    {
        var (matchPacketForSheep, matchPacketForWolf) = MakeMatchPacket(packet);
        sheepPlayer.Session?.Send(matchPacketForSheep);
        wolfPlayer.Session?.Send(matchPacketForWolf);
    }

    private async void StartTestGame(MatchSuccessPacketRequired packet)
    {
        var faction = packet.SheepUserName == "Test" ? Faction.Wolf : Faction.Sheep;
        var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
        var player = CreatePlayer(room, packet.SheepUserId, packet.SheepSessionId, faction);
        var npc = CreateNpc(player);
        var matchPacket = new S_MatchMakingSuccess
        {
            EnemyUserName = npc.Info.Name,
            EnemyRankPoint = packet.SheepRankPoint,
            EnemyCharacterId = packet.SheepCharacterId,
            EnemyAssetId = player.Faction == Faction.Sheep ? (int)packet.EnchantId : (int)packet.SheepId,
        };

        foreach (var unitId in packet.SheepUnitIds)
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
            EnemyCharacterId = packet.WolfCharacterId,
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
            EnemyCharacterId = packet.SheepCharacterId,
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
    
    public async Task<T?> GetUserIdFromApiServer<T>(string url, object? obj)
    {
        var sendUrl = $"{BaseUrl}/{url}";
        byte[]? jsonBytes = null;
        if (obj != null)
        {
            var jsonStr = JsonConvert.SerializeObject(obj);
            jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, sendUrl)
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
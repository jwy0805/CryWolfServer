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
            var sheepPlayer = ObjectManager.Instance.Add<Player>();
            sheepPlayer.Camp = Camp.Sheep;
            sheepPlayer.Info.Name = $"Player_{packet.SheepUserId}";
            sheepPlayer.Session = SessionManager.Instance.FindByUserId(packet.SheepUserId);
        
            var wolfPlayer = ObjectManager.Instance.Add<Player>();
            wolfPlayer.Camp = Camp.Wolf;
            wolfPlayer.Info.Name = $"Player_{packet.WolfUserId}";
            wolfPlayer.Session = SessionManager.Instance.FindByUserId(packet.WolfUserId);
            
            if (sheepPlayer.Session == null || wolfPlayer.Session == null)
            {
                if ((DateTime.UtcNow - startTime.Value).TotalMilliseconds > 5000)
                {
                    Console.WriteLine("Session timeout.");
                    tcs.SetResult(false);
                    return;
                }
                
                Console.WriteLine("Session is not ready yet.");
                GameLogic.Instance.PushAfter(400, () =>
                {
                    _ = RetryStartGameAsync(packet, startTime, tcs);
                });
                return;
            }
            
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            room.Push(room.EnterGame, sheepPlayer);
            room.Push(room.EnterGame, wolfPlayer);
            Console.WriteLine("Game room created.");
            tcs.SetResult(true);
        });

        return await tcs.Task;
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
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using Server.Data;

namespace Server.Game;

public class NetworkManager
{
    private readonly IGameSetupHandler _gameSetupHandler = new GameSetupHandler();
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
            _ = Task.Run(() => ProcessContextAsync(context));
        }
    }
    
    private async Task ProcessContextAsync(HttpListenerContext context)
    {
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
                    return;
            }
            
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.StatusCode = (int)HttpStatusCode.OK;
            await response.OutputStream.WriteAsync(buffer);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Http Error: {e}");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
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

        _ = _gameSetupHandler.StartRankGame(matchRequest);
        Console.WriteLine($"match request processed: {matchRequest.SheepUserId} and {matchRequest.WolfUserId}");
        var matchResponse = new MatchSuccessPacketResponse { IsSuccess = true }; 
        
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

        _ =  _gameSetupHandler.StartFriendlyGame(matchRequest);
        var matchResponse = new FriendlyMatchPacketResponse { IsSuccess = true };
        
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

        var task = await _gameSetupHandler.SurrenderGameAsync(surrenderRequest);
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
        
        var task = await _gameSetupHandler.StartTutorialAsync(tutorialRequest);
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
        
        var task = await _gameSetupHandler.StartSingleGameAsync(singleGameRequest);
        var response = new SinglePlayStartPacketResponse { SinglePlayStartOk = task };
        
        return JsonConvert.SerializeObject(response);
    }
    
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
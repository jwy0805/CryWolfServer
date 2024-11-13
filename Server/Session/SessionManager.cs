using Server.Game;

namespace Server;

public class SessionManager
{
    public static SessionManager Instance { get; } = new();

    private int _sessionId = 0;
    private Dictionary<int, ClientSession> _sessions = new();
    private readonly object _lock = new();

    public int GetBusyScore()
    {
        int count = 0;
        
        lock (_lock)
        {
            count = _sessions.Count;
        }

        return count / 100;
    }
    
    public List<ClientSession> GetSessions()
    {
        List<ClientSession> sessions;

        lock (_lock)
        {
            sessions = _sessions.Values.ToList();
        }

        return sessions;
    }
    
    public ClientSession Generate()
    {   // 클라이언트의 Connector에서 연결을 요청하고 서버에서 수락한 이후 호출됨 
        lock (_lock)
        {
            var sessionId = ++_sessionId;
            var session = new ClientSession { SessionId = sessionId };
            _sessions.Add(sessionId, session);
            Console.WriteLine($"Connected : {sessionId}");

            return session;
        }
    }

    public ClientSession? Find(int id)
    {
        lock (_lock)
        {
            _sessions.TryGetValue(id, out var session);
            return session;
        }
    }
    
    public ClientSession? FindByUserId(int userId)
    {
        lock (_lock)
        {
            return _sessions.Values.FirstOrDefault(s => s.UserId == userId);
        }
    }

    public void Remove(ClientSession session)
    {
        lock (_lock)
        {
            _sessions.Remove(session.SessionId);
        }
    }
}
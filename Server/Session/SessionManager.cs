namespace Server;

public class SessionManager
{
    private static SessionManager _session = new();
    public static SessionManager Instance => _session;

    private int _sessionId = 0;
    private Dictionary<int, ClientSession> _sessions = new();
    private readonly object _lock = new();

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
    {
        lock (_lock)
        {
            int sessionId = ++_sessionId;
            ClientSession session = new ClientSession { SessionId = sessionId };
            _sessions.Add(sessionId, session);

            Console.WriteLine($"Connected : {sessionId}");

            return session;
        }
    }

    public ClientSession Find(int id)
    {
        lock (_lock)
        {
            _sessions.TryGetValue(id, out var session);
            return session;
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
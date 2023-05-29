namespace DummyClient;

public class SessionManager
{
    private static SessionManager _session = new SessionManager();
    public static SessionManager Instance => _session;

    private List<ServerSession> _sessions = new List<ServerSession>();
    private object _lock = new object();
    private Random _rand = new Random();
    
    public void SendForEach()
    {
        lock (_lock)
        {
            foreach (ServerSession session in _sessions)
            {
                C_Move movePacket = new C_Move();
                movePacket.PosX = _rand.Next(-50, 50);
                movePacket.PosY = 0;
                movePacket.PosZ = _rand.Next(-50, 50);
                session.Send(movePacket.Write()); 
            }
        }
    }
    public ServerSession Generate()
    {
        lock (_lock)
        {
            ServerSession session = new ServerSession();
            _sessions.Add(session);
            return session;
        }
    }
}
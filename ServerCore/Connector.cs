using System.Net;
using System.Net.Sockets;

namespace ServerCore;

public class Connector
{
    private Func<Session> _sessionFactory;

    public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory = sessionFactory;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectedCompleted;
            args.RemoteEndPoint = endPoint;
            args.UserToken = socket;

            RegisterConnect(args);
        }
    }

    void RegisterConnect(SocketAsyncEventArgs args)
    {
        if (args.UserToken is not Socket socket) return;

        bool pending = socket.ConnectAsync(args);
        if (pending == false) OnConnectedCompleted(null, args);
    }

    void OnConnectedCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            Session session = _sessionFactory.Invoke();
            session.Start(args.ConnectSocket);
            session.OnConnected(args.RemoteEndPoint);
        }
        else
        {
            {
                Console.WriteLine($"OnConnectCompleted Fail: {args.SocketError}");
            }
        }
    }
}
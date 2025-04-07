using System.Net;
using System.Net.Sockets;

namespace ServerCore;

public class Listener
{
    private Socket _listenSocket;
    private Func<Session> _sessionFactory;

    public void Init(IPEndPoint endPoint, Func<Session> sessionFactory, int register = 10, int backlog = 100)
    {
        _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _sessionFactory = sessionFactory;
        _listenSocket.Bind(endPoint);
        _listenSocket.Listen(backlog);

        for (int i = 0; i < register; i++)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnAcceptCompleted;
            RegisterAccept(args);
        }
    }

    private void RegisterAccept(SocketAsyncEventArgs args)
    {
        args.AcceptSocket = null;
        bool pending = _listenSocket.AcceptAsync(args);
        if (pending == false) OnAcceptCompleted(null, args);
    }

    private void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            var socket = args.AcceptSocket;
            if (socket == null)
            {
                Console.WriteLine("Accept failed : Socket is null");
                return;
            }
            
            EndPoint? remote = socket.RemoteEndPoint;
            Session session = _sessionFactory.Invoke();
            session.Start(args.AcceptSocket);

            if (remote != null)
            {
                session.OnConnected(remote);
            }
        }
        else
        {
            Console.WriteLine(args.SocketError.ToString());
        }
        
        RegisterAccept(args);
    }
}
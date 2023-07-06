﻿using System.Net;
using Server.Game;
using ServerCore;

namespace Server;

public class Program
{
    private static Listener _listener = new Listener();

    private static void GameLogicTask()
    {
        while (true)
        {
            GameLogic.Instance.Update();
            Thread.Sleep(0);
        }
    }

    private static void NetworkTask()
    {
        while (true)
        {
            List<ClientSession> sessions = SessionManager.Instance.GetSessions();
            foreach (var session in sessions)
            {
                session.FlushSend();
            }
            
            Thread.Sleep(0);
        }
    }
    
    private static void Main(string[] args)
    {
        GameLogic.Instance.Push(() => { GameLogic.Instance.Add(1);});
        
        // DNS (Domain Name System) ex) www.naver.com -> 123.123.124.12
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress? ipAddress = null;
        foreach (var ip in ipHost.AddressList)
        {
            if (ip.ToString().Contains("172")) ipAddress = ip;
        }
        // IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        if (ipAddress != null)
        {
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);
            _listener.Init(endPoint, () => SessionManager.Instance.Generate());
            Console.WriteLine($"Listening... {endPoint}");
        }

        Task gameLogicTask = new Task(GameLogicTask, TaskCreationOptions.LongRunning);
        gameLogicTask.Start();

        Task networkTask = new Task(NetworkTask, TaskCreationOptions.LongRunning);
        networkTask.Start();
    }
}
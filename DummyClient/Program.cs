using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;

namespace DummyClient;

class Program
{
    static void Main(string[] args)
    {
        // Thread.Sleep(5000);
        // DNS (Domain Name System) ex) www.naver.com -> 123.123.124.12
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        // IPAddress ipAddress = ipHost.AddressList[0];
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);

        Connector connector = new Connector();
        connector.Connect(endPoint, () => { return SessionManager.Instance.Generate(); }, 10);
        
        while (true)
        {
            try
            {
                SessionManager.Instance.SendForEach();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Thread.Sleep(250);
        }
    }
}


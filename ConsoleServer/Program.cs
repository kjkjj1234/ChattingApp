using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ConsoleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Echo();
        }
        public class Server
        {
            public const int PORT = 5555;
            TcpListener listener = null;
            public static ArrayList handleList = new ArrayList(10);
            public Server()
            {
                handleList.Clear();
            }
            public void Echo()
            {
                try
                {
                    IPAddress address = Dns.GetHostEntry("").AddressList[0];
                    //listener = new TcpListener(address, PORT);
                    listener = new TcpListener(IPAddress.Any, PORT);
                    listener.Start();
                    Console.WriteLine("Server ready 1-------");
                    while (true)
                    {
                        TcpClient client = listener.AcceptTcpClient(); //클라이언트 개당 소켓생성
                        EchoHandler handler = new EchoHandler(this, client);
                        Add(handler);
                        handler.start();
                    }
                }
                catch (Exception ee)
                {
                    Console.WriteLine("2--------------------");
                    System.Console.WriteLine(ee.Message);
                }
                finally
                {
                    Console.WriteLine("3--------------------");
                    listener.Stop();
                }
            }
            public void Add(EchoHandler handler)
            {
                lock (handleList.SyncRoot)
                    handleList.Add(handler);
            }
            public void broadcast(String str)
            {
                lock (handleList.SyncRoot)
                {
                    string dstes = DateTime.Now.ToString() + " : ";
                    Console.Write(dstes);
                    Console.WriteLine(str);
                    foreach (EchoHandler handler in handleList)
                    {
                        EchoHandler echo = handler as EchoHandler;
                        if (echo != null)
                            echo.sendMessage(str);
                    }
                }
            }
            public void Remove(EchoHandler handler)
            {
                lock (handleList.SyncRoot)
                    handleList.Remove(handler);
            }
        }
        public class EchoHandler
        {
            Server server;
            TcpClient client;
            NetworkStream ns = null;
            StreamReader sr = null;
            StreamWriter sw = null;
            string str = string.Empty;

            string clientName;
            public EchoHandler(Server server, TcpClient client)
            {
                this.server = server;
                this.client = client;
                try
                {
                    ns = client.GetStream();
                    Socket socket = client.Client;
                    clientName = socket.RemoteEndPoint.ToString();
                    Console.WriteLine(clientName + " 접속");
                    sr = new StreamReader(ns, Encoding.Default);
                    sw = new StreamWriter(ns, Encoding.Default);
                }
                catch (Exception) { Console.WriteLine("연결 실패"); }
            }
            public void start()
            {
                Thread t = new Thread(new ThreadStart(ProcessClient));
                t.Start();
            }
            public void ProcessClient()
            {
                try
                {
                    while ((str = sr.ReadLine()) != null)
                        server.broadcast(str);
                }
                catch (Exception)
                {
                    Console.WriteLine(clientName + " 접속해제");
                    sw.Flush();
                }
                finally
                {
                    server.Remove(this);
                    sw.Close();
                    sr.Close();
                    client.Close();
                }
            }
            public void sendMessage(string message)
            {
                sw.WriteLine(message);
                sw.Flush();
            }
        }
    }
}
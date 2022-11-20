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
            // 생성자 호출, 힙에 객체 생성
            Server server = new Server();
            server.Echo();
        }
        public class Server
        {
            // PORT: 네트워크 서비스나 특정 프로세스를 식별하는 논리 단위이다.
            public const int PORT = 5555;
            // TCPListener 클래스: 클라이언트의 연결 요청을 기다리는 역할을 한다.
            TcpListener listener = null;
            // ArrayList: 배열과 유사한 컬렉션. 크기가 자동으로 늘어나며, 타입이 서로 다른 값을 추가할 수 있다.
            public static ArrayList handleList = new ArrayList(10);
            public Server()
            {
                // handleList에서 요소를 모두 제거한다.
                handleList.Clear();
            }
            public void Echo()
            {
                try
                {
                    // IPAddress: IP주소를 나타낸다.
                    // Dns.GetHostEntry(): 호스트명에 대한 IP 정보, Alias 정보 등을 리턴한다.
                    // 리턴되는 정보를 address에 담는다. 해당 호스트에 대한 IP 주소는 복수 개일 수 있으므로 AddressList[] 배열의 형태로 저장한다.
                    IPAddress address = Dns.GetHostEntry("").AddressList[0];
                    // IPAddress.Any: 사용 중인 '모든' 네트워크 인터페이스(랜카드에 할당된 IP 주소)를 나타낸다.
                    listener = new TcpListener(IPAddress.Any, PORT);
                    // listener 객체는 클라이언트가 TcpClient.Connect()를 호출하여 연결 요청이 오기를 기다린다.
                    listener.Start();
                    Console.WriteLine("Server ready 1-------");
                    while (true)
                    {
                        //클라이언트 개당 소켓생성
                        TcpClient client = listener.AcceptTcpClient();
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
                    // GetStream(): 소켓에서 메시지를 가져오는 스트림
                    ns = client.GetStream();
                    Socket socket = client.Client;
                    // clientName 부분에 로그인 정보에서 닉네임만 연결해주면 될 것 같아요.
                    // 연결 무사히 되면 디자인 Form1에서 txtName 텍스트박스 속성 중 ReadOnly를 true로 변경해주세요.
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
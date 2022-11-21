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
            // 서버 준비
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
                        // .AcceptTcpClient(): 보류 중인 연결 요청을 수락한다.
                        //클라이언트 개당 소켓 생성
                        TcpClient client = listener.AcceptTcpClient();
                        // this를 인수로 전달함.
                        EchoHandler handler = new EchoHandler(this, client);
                        // Add 함수에 handler를 전달해줌.
                        Add(handler);
                        // handler를 시작함.
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
                // lock(): 특정 스레드 객체가 A 메서드를 호출하고 있으면, 다른 스레드 객체는 A 메서드에 접근할 수 없도록 한다.
                // .SyncRoot: 여러 스레드가 데이터에 액세스하고 공유할 수 있도록 하는 데 사용한다.
                // handleList를 한 스레드가 호출하고 있을 때 다른 스레드는 접근할 수 없도록 잠금.
                lock (handleList.SyncRoot)
                    // handleList에 handler를 추가함.
                    handleList.Add(handler);
            }
            public void broadcast(String str)
            {
                // handleList를 한 스레드가 호출하고 있을 때 다른 스레드는 접근할 수 없도록 잠금.
                lock (handleList.SyncRoot)
                {
                    // 현재 날짜, 시간을 출력함.
                    string dstes = DateTime.Now.ToString() + " : ";
                    Console.Write(dstes);
                    // 빈 문자열 출력
                    Console.WriteLine(str);
                    // foreach (데이터형식 변수명 in 배열): 배열의 끝에 도달하면 자동으로 반복이 종료됨.
                    foreach (EchoHandler handler in handleList)
                    {
                        // as: 형변환이 가능하면 형변환을 수행하고, 그렇지 않으면 null 값을 대입하는 연산자다. 
                        EchoHandler echo = handler as EchoHandler;
                        // 형변환이 가능하다면 메시지를 출력함.
                        if (echo != null)
                            echo.sendMessage(str);
                    }
                }
            }
            public void Remove(EchoHandler handler)
            {
                // handleList를 한 스레드가 호출하고 있을 때 다른 스레드는 접근할 수 없도록 잠금.
                lock (handleList.SyncRoot)
                    // .Remove(): 특정 요소를 리스트에서 제거 (객체 지정)
                    handleList.Remove(handler);
            }
        }
        public class EchoHandler
        {
            Server server;
            // TcpClient 클래스는 클라이언트에서는 TcpClient가 서버에 연결 요청을 하는 역할을 한다.
            // 서버에서는 클라이언트의 요청을 수락하면 클라이언트와 통신을 할 때 사용하는 TcpClient의 인스턴스가 반환된다.
            TcpClient client;
            // NetworkStream: 데이터를 주고 받는데 사용한다.
            NetworkStream ns = null;
            // StreamReader: 스트림에서 문자를 읽는다.
            StreamReader sr = null;
            // StreamWriter: 문자열 데이터를 스트림에 저장하는 데 쓰인다.
            StreamWriter sw = null;
            // 비어있는 문자열
            string str = string.Empty;
            string clientName;
            public EchoHandler(Server server, TcpClient client)
            {
                // this: 클래스 내부에서 필드명과, 메서드의 매개 변수의 이름이 동일할 때 모호성을 제거할 수 있다.
                this.server = server;
                this.client = client;
                try
                {
                    // GetStream(): 소켓에서 메시지를 가져오는 스트림
                    ns = client.GetStream();
                    Socket socket = client.Client;
                    // clientName 부분에 로그인 정보에서 닉네임만 연결해주면 될 것 같아요.
                    // 연결 무사히 되면 디자인 Form1에서 txtName 텍스트박스 속성 중 ReadOnly를 true로 변경해주세요.

                    // .RemoteEndPoint: Socket의 원격 EndPoint(종단점) 정보를 조사한다.
                    // EndPoint: 서버와 클라이언트간에 통신을 하기위한 양쪽 터널의 끝점같은 것.
                    clientName = socket.RemoteEndPoint.ToString();
                    Console.WriteLine(clientName + " 접속");
                    // Encoding.Default: Default byte에서 string으로 변환해준다.
                    // 읽기, 쓰기 설정
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
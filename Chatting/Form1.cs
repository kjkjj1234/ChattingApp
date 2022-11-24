using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Chatting
{
    public partial class Form1 : Form
    {
        string serverIP, dialogName;
        int serverPort;
        bool isAlive = false;
        // NetworkStream: 데이터를 주고 받는데 사용한다.
        NetworkStream ns = null;
        // StreamReader: 스트림에서 문자를 읽는다.
        StreamReader sr = null;
        // StreamWriter: 문자열 데이터를 스트림에 저장하는 데 쓰인다.
        StreamWriter sw = null;
        // TcpClient 클래스는 클라이언트에서는 TcpClient가 서버에 연결 요청을 하는 역할을 한다.
        // 서버에서는 클라이언트의 요청을 수락하면 클라이언트와 통신을 할 때 사용하는 TcpClient의 인스턴스가 반환된다.
        TcpClient client = null;

        // 아이피 정보 저장
        public Form1()
        {
            InitializeComponent();
            // Dns.GetHostByName: 인터넷 DNS 서버에서 호스트 정보를 캐낸다. 빈 문자열을 호스트 이름으로 전달하면 이 메서드는 로컬 컴퓨터의 표준 호스트 이름을 검색한다.
            IPHostEntry hostIP = Dns.GetHostByName(Dns.GetHostName());
            // hostIP.AddressList : 호스트와 연결된 IP 주소를 가져오거나 설정한다.
            serverIP = hostIP.AddressList[0].ToString();
        }
        // 폼을 띄웠을 때 포트 번호랑 아이피 번호 나타냄.
        private void Form1_Load(object sender, EventArgs e)
        {
            this.txtPort.Text = 5555.ToString();
            this.txtIP.Text = serverIP.ToString();
            // .Enabled : 컨트롤이 사용자 상호 작용에 응답할 수 있으면 true이고, 그렇지 않으면 false이다.
            this.txtPort.Enabled = false;
            this.txtIP.Enabled = false;
        }
        // 클라이언트가 연결하려고 할 때 적어야 하는 필수 내용. 필수 내용을 전부 입력했다면 입장 메시지 출력.
        private void btnConnect_Click(object sender, EventArgs e)
        {
            dialogName = this.txtName.Text;
            // .IsNullOrEmpty : 공백과 null 값을 체크해야 되는 경우에 사용하는 string 클래스 함수이다. 해당 예외처리는 필수값 체크 시 많이 사용되며, 필수값이 없을때 예외처리를 해주기 위해 자주 사용하는 함수이다.
            if (string.IsNullOrEmpty(dialogName))
            {
                MessageBox.Show("대화명을 입력하세요.");
                return;
            }
            if (string.IsNullOrEmpty(serverIP))
            {
                MessageBox.Show("주소를 입력하세요.");
                return;
            }
            if (string.IsNullOrEmpty(this.txtPort.Text))
            {
                MessageBox.Show("포트를 입력하세요.");
                return;
            }
            // .Parse(): 숫자 형식의 문자열을 정수로 변환할 수 있다.
            // Int32.Parse(): 32비트 부호 있는 정수 타입에 사용할 수 있다.
            serverPort = Int32.Parse(this.txtPort.Text);
            // isAlive 프로퍼티: 현재 스레드의 실행 상태를 나타낸다. 스레드가 시작된 경우 true를 반환하고 그렇지 않은 경우 false를 반환한다.
            isAlive = true;
            try
            {
                this.Echo();
                sendMessage("[" + dialogName + " 입장]");
            }
            catch (Exception)
            {
                // .Clear : 지우기 메서드
                this.txtName.Clear();
                // isAlive 프로퍼티 : 현재 스레드의 실행 상태를 나타낸다. 스레드가 시작된 경우 true를 반환하고 그렇지 않은 경우 false를 반환한다.
                this.isAlive = false;
            }
        }
        // 메시지를 입력하고 엔터키를 누르자마자 대화로그에 전달.
        private void txtSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            // KeyChar: 사용자가 누른 키의 실제 문자 값을 반환
            if ((int)Keys.Enter == e.KeyChar)
            {
                string message = this.txtSend.Text;
                // .Trim: 현재 문자열의 앞쪽, 뒤쪽 공백을 모두 제거한 문자열을 반환한다.
                // [클라이언트 이름]보낸 메시지
                sendMessage("[" + dialogName + "] " + message.Trim());
                // .Clear : 지우기 메서드
                this.txtSend.Clear();
                // .SelectionStart : 텍스트 상자에서 선택한 텍스트의 시작 지점을 가져오거나 설정한다.
                this.txtSend.SelectionStart = 0;
            }
        }
        // 종료하기 버튼을 누르면 퇴장을 대화로그에 전달한 후 폼 종료.
        private void btnExit_Click(object sender, EventArgs e)
        {
            try
            {
                sendMessage("[" + dialogName + " 퇴장]");
                // .Close : 폼 닫기
                sr.Close();
                sw.Close();
                ns.Close();
            }
            catch { }
            finally
            {
                // Dispose : 바로 삭제가 필요한 리소스를 해제하는 함수
                this.Dispose();
            }
        }
        // 클라이언트 정보를 서버에 전달해줌.
        public void Echo()
        {
            try
            {
                // 클라이언트 연결
                client = new TcpClient(this.serverIP, this.serverPort);
                // GetStream(): 소켓에서 메시지를 가져오는 스트림
                ns = client.GetStream();
                // Encoding.Default: Default byte에서 string으로 변환해준다.
                // 읽기 설정
                sr = new StreamReader(ns, Encoding.Default);
                // 쓰기 설정
                sw = new StreamWriter(ns, Encoding.Default);
                // thread: 프로세스 내부에서 생성되는 실제로 작업을 하는 주체이다.
                // 새로운 쓰레드에서 run() 실행
                Thread receiveThread = new Thread(new ThreadStart(run));
                // .IsBackground : 메인 프로세스가 종료될 때 Thread도 같이 종료됨.
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("서버 시작 실패");
                // throw: 특정 조건을 만족하지 않으면 throw 문을 통해서 예외를 던지고 catch 문으로 받는다.
                throw e;
            }
        }
        // 클라이언트가 연결되어 있으면 계속 실행.
        public void run()
        {
            string message = "start";
            try
            {
                // .Connected : Socket이 마지막으로 Send 또는 Receive 작업을 수행할 때 원격 호스트에 연결되었는지 여부를 나타내는 값을 가져온다.
                // 원격 호스트에 연결되었는지와 읽을 수 있는지를 확인.
                if (client.Connected && sr != null)
                    // .ReadLine: 현재 스트림에서 한 줄의 문자를 읽고 데이터를 문자열로 반환한다.
                    while ((message = sr.ReadLine()) != null)
                        AppendMessage(message);
            }
            catch (Exception) { MessageBox.Show("error"); }
        }
        // 대화로그 설정 부분
        public void AppendMessage(string message)
        {
            if (this.txtDialog != null && this.txtSend != null)
            {
                // .AppendText : 항상 스크롤이 BOTTOM 으로 가게된다.
                // \r: 맨 앞, \n: 다음 줄
                this.txtDialog.AppendText(message + "\r\n");
                this.txtDialog.Focus();
                // 글을 계속 입력받을 때 입력받은 마지막 줄에 포커스를 맞춰준다.
                this.txtDialog.ScrollToCaret();
            }
        }
        // 메시지 전송
        private void sendMessage(string message)
        {
            try
            {
                if (sw != null)
                {
                    sw.WriteLine(message);
                    // .Flush : 버퍼된 바이트를 모두 출력하여 버퍼를 비우하는 것을 명시하는 메소드이다.
                    sw.Flush();
                }
            }
            catch (Exception) { MessageBox.Show("전송실패"); }
        }
    }
}
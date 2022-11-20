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

        public Form1()
        {
            InitializeComponent();
            IPHostEntry hostIP = Dns.GetHostByName(Dns.GetHostName());
            serverIP = hostIP.AddressList[0].ToString();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.txtPort.Text = 5555.ToString();
            this.txtIP.Text = serverIP.ToString();
            this.txtPort.Enabled = false;
            this.txtIP.Enabled = false;
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            dialogName = this.txtName.Text;
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
            serverPort = Int32.Parse(this.txtPort.Text);
            isAlive = true;
            try
            {
                this.Echo();
                sendMessage("[" + dialogName + " 입장]");
            }
            catch (Exception)
            {
                this.txtName.Clear();
                this.isAlive = false;
            }
        }
        private void txtSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((int)Keys.Enter == e.KeyChar)
            {
                string message = this.txtSend.Text;
                sendMessage("[" + dialogName + "] " + message.Trim());
                this.txtSend.Clear();
                this.txtSend.SelectionStart = 0;
            }
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            try
            {
                sendMessage("[" + dialogName + " 퇴장]");
                sr.Close();
                sw.Close();
                ns.Close();
            }
            catch { }
            finally
            {
                this.Dispose();
            }
        }
        public void Echo()
        {
            try
            {
                // 클라이언트 연결
                client = new TcpClient(this.serverIP, this.serverPort);
                // GetStream(): 소켓에서 메시지를 가져오는 스트림
                ns = client.GetStream();
                // 메시지를 받아옴
                sr = new StreamReader(ns, Encoding.Default);
                // 메시지를 보냄
                sw = new StreamWriter(ns, Encoding.Default);
                Thread receiveThread = new Thread(new ThreadStart(run));
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("서버 시작 실패");
                throw e;
            }
        }
        public void run()
        {
            string message = "start";
            try
            {
                if (client.Connected && sr != null)
                    while ((message = sr.ReadLine()) != null)
                        AppendMessage(message);
            }
            catch (Exception) { MessageBox.Show("error"); }
        }
        public void AppendMessage(string message)
        {
            if (this.txtDialog != null && this.txtSend != null)
            {
                this.txtDialog.AppendText(message + "\r\n");
                this.txtDialog.Focus();
                // 글을 계속 입력받을 때 입력받은 마지막 줄에 포커스를 맞춰준다.
                this.txtDialog.ScrollToCaret();
            }
        }
        private void sendMessage(string message)
        {
            try
            {
                if (sw != null)
                {
                    sw.WriteLine(message);
                    sw.Flush();
                }
            }
            catch (Exception) { MessageBox.Show("전송실패"); }
        }
    }
}
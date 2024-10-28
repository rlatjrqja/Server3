using System.Net;
using System.Net.Sockets;
using Protocols;

namespace ServerSocket
{
    public class RootServer
    {
        public RootServer instance; //{ get { return instance; } private set { instance = this; } }
        Socket? host;

        public RootServer(string IP, int PORT)
        {
            /// 싱글톤 패턴으로 객체 생성
            if(instance == null)
            {
                instance = this;
                IPEndPoint serverIP = new IPEndPoint(IPAddress.Parse(IP), PORT);
                host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                host.Bind(serverIP);
            }
        }

        public void StartServer()
        {
            /// 3way-hanshake가 가능한 상태
            host.Listen();
        }

        public Socket? AddUsers()
        {
            Socket remote = host.Accept();

            /// 소켓에서 받아서 3way-handshake 완료
            byte[] buffer = new byte[1024];
            int length = remote.Receive(buffer, buffer.Length, SocketFlags.None);

            /// OPCODE 000을 기다림
            Header hd = new Header();
            hd.ByteToHeader(buffer);
            if(hd.OPCODE == 000)
            {
                return remote;
            }
            else return null;
        }
    }
}

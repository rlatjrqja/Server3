using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server_main
{
    public class RootServer
    {
        public static RootServer instance; // 서버는 단 하나만 존재
        Socket host; // 서버의 호스트
        public List<ClientHandle> users = new List<ClientHandle>(); // 이 서버에서 관리하는 클라이언트 인터페이스

        public RootServer(string IP, int PORT)
        {
            /// 싱글톤 패턴으로 객체 생성
            if (instance == null)
            {
                instance = this;
                IPEndPoint serverIP = new IPEndPoint(IPAddress.Parse(IP), PORT);
                host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                host.Bind(serverIP);
            }
        }

        public bool StartServer()
        {
            /// 3way-hanshake가 가능한 상태
            if (host != null)
            {
                /// 서버 상태 전환
                host.Listen();
                return true;
            }
            else return false;
        }

        public ClientHandle RunServer()
        {
            Socket client = host.Accept();
            ClientHandle handle = new ClientHandle(client);

            return handle;
        }

        public int GetUserCount() { return users.Count; }
    }
}

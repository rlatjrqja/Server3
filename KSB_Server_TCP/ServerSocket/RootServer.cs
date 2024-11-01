﻿using System.Net;
using System.Net.Sockets;
using Protocols;

namespace ServerSocket
{
    public class RootServer
    {
        public RootServer instance;
        List<ClientHandle> users = new List<ClientHandle>();
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

        public bool StartServer()
        {
            /// 3way-hanshake가 가능한 상태
            if(host != null)
            {
                host.Listen();
                return true;
            }
            else return false;
        }

        public bool RunServer()
        {
            Socket? client = AddUsers();

            /// OPCODE 000을 받았다면 Handle로 발전시켜 관리
            if (client != null)
            {
                ClientHandle handle = new ClientHandle(client);
                users.Add(handle);
                handle.StartListening();
                Console.WriteLine($"{DateTime.Now}_New Client Added. From~[{client.RemoteEndPoint}]");
            }
            return true;
        }

        public Socket? AddUsers()
        {
            Socket remote = host.Accept();

            /// 소켓에서 받아서 3way-handshake 완료
            byte[] buffer = new byte[1024];
            int length = remote.Receive(buffer, buffer.Length, SocketFlags.None);

            /// OPCODE 000을 기다림
            Protocol protocol = new Protocol();
            protocol.MakeHeader(buffer);

            /// 수신받은 OPCODE 000에 따라 되돌려주기
            if (protocol.OPCODE == 000)
            {
                byte[] response = protocol.StartConnectionResponse(true);
                remote.Send(response);
                return remote;
            }
            else
            {
                byte[] response = protocol.StartConnectionResponse(false);
                remote.Send(response);
                return null;
            }
        }

        public int GetUserCount() { return users.Count; }
    }
}

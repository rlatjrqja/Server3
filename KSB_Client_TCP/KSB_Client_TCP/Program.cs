using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocols;

namespace KSB_Client_TCP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Socket host;

            //string ip = "172.18.27.199";
            string ip = "192.168.45.232";
            int port = 50000;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            host.Connect(ipep);

            /// 접속 요청
            {
                byte[] body = Encoding.UTF8.GetBytes("Connection Request");
                byte[] request = Header.MakePacket(0, 000, 0, body.Length, 0, body);
                host.Send(request);
                Console.WriteLine($"서버 접속 요청 [Length]:{request.Length}");
            }

            /// 응답 대기
            {
                Header header = WaitForServerResponse(host);
                if(!CheckOPCODE(header, 000, "서버 접속 성공", "서버 접속 실패")) return;
            }


            /// 파일 전송 프로토콜
            {
                string root = @"..\..\..\..\..\SendingFile";
                string name = @"\Dummy.xlsx";

                string filename = root + name;
                string fullPath = Path.GetFullPath(filename);
                var file = new FileInfo(fullPath);
                byte[] binary = new byte[file.Length]; // 바이너리 버퍼

                if (file.Exists)
                {
                    var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                    stream.Read(binary, 0, binary.Length);
                }

                byte[] request = Protocol1_File.TransmitFileRequest(file.Name.Length, file.Name, binary.Length);
                byte[] data = Header.MakePacket(0, 100, 0, request.Length, 0, request);
                host.Send(data);
                Console.WriteLine("파일 전송 가능 상태 확인");

                Header header = WaitForServerResponse(host);
                if (CheckOPCODE(header, 100, "파일 전송 가능", "파일 전송 불가"))
                {
                    // 파일 보내도 된다 (100 OK) 받고 파일 전송
                    List<byte[]> packets = Protocol1_File.TransmitFile(binary);
                    for (int i = 0; i < packets.Count; i++)
                    {
                        host.Send(Header.MakePacket(0, 200, i, packets[i].Length, 0, packets[i]));
                        Console.WriteLine($"[Send] {packets[i].Length} Byte");
                    }
                }
            }
        }

        private static Header WaitForServerResponse(Socket host)
        {
            byte[] buffer = new byte[4096];
            int bytesReceived = host.Receive(buffer);
            if (bytesReceived > 0)
            {
                Header header = new Header();
                header.MakeHeader(buffer);
                return header;
            }
            return null;
        }

        private static bool CheckOPCODE(Header hd, int opcode, string correctMSG, string failMSG)
        {
            if(hd.OPCODE == opcode)
            {
                Console.WriteLine(correctMSG);
                return true;
            }
            else
            {
                Console.WriteLine(failMSG);
                return false;
            }
        }
    }
}

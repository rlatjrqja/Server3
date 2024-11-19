using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocols;
using Encryption;

namespace KSB_Client_TCP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ip = "172.18.27.199";
            //string ip = "192.168.45.232";
            int port = 50001;
            string rootDir = @"..\..\..\..\..\SendingFile";
            string name = @"\Dummy.xlsx";

            // 기존 설정 코드
            Socket host = ConnectTo(ip, port);

            Header response_connect = WaitForServerResponse(host);
            if (!CheckOPCODE(response_connect, 000, "서버 접속 성공", "서버 접속 실패")) return;

            // 파일 전송 프로토콜
            {
                // 1. 파일 바이너리 변환
                byte[] binary = FileToBinary(rootDir, name);

                // 2. 데이터 암호화
                AES aes = new AES(); // 암호화 클래스 인스턴스 생성
                byte[] encryptedBinary = aes.EncryptData(binary);
                Console.WriteLine($"파일 암호화 완료, 크기: {encryptedBinary.Length} 바이트");

                // 3. 파일 전송 요청
                byte[] request = Protocol1_File.TransmitFileRequest(name.Length, name, encryptedBinary.Length);
                byte[] data = Header.MakePacket(0, 100, 0, request.Length, 0, request);
                host.Send(data);
                Console.WriteLine("파일 전송 가능 상태 확인");

                Header response_file = WaitForServerResponse(host);
                if (CheckOPCODE(response_file, 100, "파일 전송 가능", "파일 전송 불가"))
                {
                    // 4. 암호화된 파일 전송
                    List<byte[]> packets = Protocol1_File.TransmitFile(encryptedBinary);
                    for (int i = 0; i < packets.Count; i++)
                    {
                        host.Send(Header.MakePacket(0, 200, i, packets[i].Length, 0, packets[i]));
                        Console.WriteLine($"[Send] {packets[i].Length} Byte");
                    }
                }
            }
        }


        private static Socket ConnectTo(string ip, int port)
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            host.Connect(ipep);

            byte[] body = Encoding.UTF8.GetBytes("Connection Request");
            byte[] request = Header.MakePacket(0, 000, 0, body.Length, 0, body);
            host.Send(request);
            Console.WriteLine($"서버 접속 요청 [Length]:{request.Length}");

            return host;
        }

        private static byte[] FileToBinary(string root, string name)
        {
            string filename = root + name;
            string fullPath = Path.GetFullPath(filename);
            var file = new FileInfo(fullPath);
            byte[] binary = new byte[file.Length]; // 바이너리 버퍼

            if (file.Exists)
            {
                var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                stream.Read(binary, 0, binary.Length);
            }

            return binary;
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

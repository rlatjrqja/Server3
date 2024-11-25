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
            //string ip = "192.168.45.232"; // 고정 IP
            string ip = "127.0.0.1"; // 고정 IP
            int port = 50001;            // 고정 포트 번호
            string rootDir = @"..\..\..\..\..\SendingFile";
            string name = @"\Dummy.xlsx";

            // 클라이언트 소켓 생성 및 연결
            Socket host = ConnectTo(ip, port);

            Header response_connect = WaitForServerResponse(host);
            if (!CheckOPCODE(response_connect, 000, "서버 접속 성공", "서버 접속 실패")) return;

            bool running = true;

            while (running)
            {
                Console.Clear();
                Console.WriteLine("==== 클라이언트 메뉴 ====");
                Console.WriteLine("1. 연결 확인");
                Console.WriteLine("2. Plane Text 전송");
                Console.WriteLine("3. 파일 전송");
                Console.WriteLine("4. 연결 끊기");
                Console.Write("메뉴를 선택하세요: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        // 연결 확인 - 아직 미구현
                        Console.WriteLine("연결 확인 기능은 아직 구현되지 않았습니다.");
                        break;

                    case "2":
                        // Plane Text 전송 - 아직 미구현
                        Console.WriteLine("Plane Text 전송 기능은 아직 구현되지 않았습니다.");
                        break;

                    case "3":
                        // 파일 전송
                        Console.WriteLine("파일 전송을 시작합니다...");
                        FileTransfer(host, rootDir, name);
                        break;

                    case "4":
                        // 연결 끊기
                        Console.WriteLine("서버와 연결을 종료합니다...");
                        Disconnect(host);
                        running = false;
                        break;

                    default:
                        Console.WriteLine("잘못된 입력입니다. 다시 선택하세요.");
                        break;
                }

                if (running)
                {
                    Console.WriteLine("\n계속하려면 Enter 키를 누르세요...");
                    Console.ReadLine();
                }
            }
        }

        static void FileTransfer(Socket host, string rootDir, string name)
        {
            byte[] binary = FileToBinary(rootDir, name);
            Console.WriteLine($"파일 크기: {binary.Length} 바이트");

            // 파일 전송 요청
            AES aes = new AES();
            byte[] request = Protocol1_File.TransmitFileRequest(name.Length, name, binary.Length);
            byte[] data = Header.MakePacket(0, 100, 0, request.Length, 0, request);
            host.Send(data);
            Console.WriteLine("파일 전송 가능 상태 확인");

            Header response_file = WaitForServerResponse(host);
            if (CheckOPCODE(response_file, 100, "파일 전송 가능", "파일 전송 불가"))
            {
                List<byte[]> packets = Protocol1_File.TransmitFile(binary);
                for (int i = 0; i < packets.Count; i++)
                {
                    byte[] encryptedSegment = aes.EncryptData(packets[i]);

                    // 암호화된 패킷 전송
                    host.Send(Header.MakePacket(0, 200, i, encryptedSegment.Length, 0, encryptedSegment));
                    Console.WriteLine($"[Send] {encryptedSegment.Length} Byte (Packet {i + 1}/{packets.Count})");
                }
            }

            if (CheckOPCODE(response_file, 300, "파일 전송 완료", "파일 전송 실패"))
            {
                Console.WriteLine("파일 전송을 성공적으로 완료했습니다.");
            }
            else
            {
                Console.WriteLine("파일 전송 중 오류가 발생했습니다.");
            }
        }

        static void Disconnect(Socket host)
        {
            byte[] disconnectPacket = Header.MakePacket(0, 500, 0, 0, 0, new byte[0]);
            host.Send(disconnectPacket);
            host.Close();
            Console.WriteLine("서버와의 연결을 종료했습니다.");
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

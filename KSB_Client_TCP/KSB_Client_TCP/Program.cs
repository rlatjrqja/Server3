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
            Protocol protocol = new Protocol();

            // Socket 수준에서 연결
            string ip = "172.18.27.199";
            int port = 50000;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ipep);

            // 연결 요청
            {
                byte[] request = protocol.StartConnectionRequest();
                client.Send(request);
                Console.WriteLine($"서버 접속 요청 [Length]:{request.Length}");

                byte[] response = new byte[1024];
                client.Receive(response);
                string response_s = Encoding.UTF8.GetString(response);
                Console.WriteLine($"서버 응답 수신 완료! {response_s}");

                uint OPCODE = BitConverter.ToUInt32(response, 1);
                if (OPCODE != 000)
                {
                    Console.WriteLine("서버 접속 거부");
                    return;
                }
            }

            // 보낼 파일 준비
            {
                FileConverter cv = new FileConverter();
                string filename = @"..\..\..\..\..\SendingFile\Dummy2.txt";
                string fullPath = Path.GetFullPath(filename);
                var file = new FileInfo(fullPath);
                var binary = cv.FileToByte(file);

                byte[] request = protocol.TransmitFileRequest(file.Name.Length, file.Name, binary.Length);
                client.Send(request);
                Console.WriteLine("파일 전송 가능 상태 확인");

                byte[] response = new byte[1024];
                int length = client.Receive(response);
                string msg = Encoding.UTF8.GetString(response, protocol.GetSizeHeader(), length);
                Console.WriteLine($"서버 응답: {msg}");
                uint OPCODE = BitConverter.ToUInt32(response, 1);
                if (OPCODE != 100)
                {
                    Console.WriteLine("파일 전송 불가");
                    return;
                }

                // 파일 보내도 된다 (200 OK) 받고 파일 전송
                byte[] packet = protocol.TransmitFile(binary);
                client.Send(packet);
                Console.WriteLine(packet.Length);
            }


            /*{
                // 파일 이름 크기를 보낸다.
                client.Send(BitConverter.GetBytes(file.Name.Length));
                Console.WriteLine(file.Name.Length);

                // 파일 이름을 보낸다.
                client.Send(Encoding.UTF8.GetBytes(file.Name));
                Console.WriteLine(file.Name);

                // 파일 크기를 보낸다.
                client.Send(BitConverter.GetBytes((long)binary.Length));
                Console.WriteLine(binary.Length);
            }*/

            while (true)
            {
                byte[] response = new byte[1024];
                client.Receive(response);
                Console.WriteLine(BitConverter.ToInt32(response));
                switch (BitConverter.ToInt32(response))
                {

                    case 100:
                        Console.WriteLine("파일 전송 성공");
                        client.Send(BitConverter.GetBytes(400));
                        break;
                    case 101:
                        Console.WriteLine("파일명으로 인한 전송 실패");
                        break;
                    case 102:
                        Console.WriteLine("파일로 인한 전송 실패");
                        break;
                    case 500:
                        return;
                    default:
                        Console.WriteLine("디버깅");
                        break;
                }

            }
        }

        static void ReceiveAlways(Socket socket)
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytesReceived = socket.Receive(buffer);
                    if (bytesReceived > 0)
                    {
                        var msg = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        Console.Write(msg);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"수신 중 오류 발생: {ex.Message}");
            }
        }
    }
}

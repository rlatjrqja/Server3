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
            Socket client;
            Protocol protocol = new Protocol();
            string root = @"..\..\..\..\..\SendingFile";
            string name = @"\Dummy.xlsx";

            try
            {
                // Socket 수준에서 연결
                //string ip = "172.18.27.199";
                string ip = "192.168.45.232";
                int port = 50000;
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), port);
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ipep);



                Task.Run(() => 
                {
                    while (true)
                    {
                        if(ReceivePacket(client, protocol))
                        {
                            switch (protocol.OPCODE)
                            {
                                case 000:
                                    Console.WriteLine("서버 접속 성공");
                                    break;
                                case 100:
                                    Console.WriteLine("파일 전송 가능");
                                    //client.Send(BitConverter.GetBytes(400));
                                    break;
                                case 101:
                                    Console.WriteLine("파일명으로 인한 전송 실패");
                                    break;
                                case 102:
                                    Console.WriteLine("파일 크기로 인한 전송 실패");
                                    break;
                                case 200:

                                    break;
                                case 500:
                                    return;
                                default:
                                    Console.WriteLine("디버깅");
                                    break;
                            }
                        }
                    }
                });
                

                // 연결 요청
                int count = 0;
                while (count++ < 5)
                {
                    byte[] request = protocol.StartConnectionRequest();
                    client.Send(request);
                    Console.WriteLine($"서버 접속 요청 [Length]:{request.Length}");

                    if (protocol.OPCODE == 000) break;
                    else Thread.Sleep(1000);
                }
                count = 0;

                // 보낼 파일 준비
                while (count++ < 5)
                {
                    //FileConverter cv = new FileConverter();
                    string filename = root + name;
                    string fullPath = Path.GetFullPath(filename);
                    var file = new FileInfo(fullPath);
                    byte[] binary = new byte[file.Length]; // 바이너리 버퍼

                    ///
                    // 파일이 존재하는지
                    if (file.Exists)
                    {
                        var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);

                        // 파일을 IO로 읽어온다.
                        stream.Read(binary, 0, binary.Length);
                        //return binary;
                    }
                    //else return null;
                    ///

                    byte[] request = protocol.TransmitFileRequest(file.Name.Length, file.Name, binary.Length);
                    client.Send(request);
                    Console.WriteLine("파일 전송 가능 상태 확인");

                    if (protocol.OPCODE == 100)
                    {
                        // 파일 보내도 된다 (100 OK) 받고 파일 전송
                        List<byte[]> packets = protocol.TransmitFile(binary);
                        for(int i = 0;i<packets.Count;i++)
                        {
                            client.Send(packets[i]);
                            Console.WriteLine($"[Send] { packets[i].Length} Byte");
                        }
                        break;
                    }
                    else Thread.Sleep(1000);
                }

                while (true)
                {

                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        static bool ReceivePacket(Socket socket, Protocol protocol)
        {
            byte[] buffer = new byte[1024];
            try
            {
                int bytesReceived = socket.Receive(buffer);
                if (bytesReceived > 0)
                {
                    protocol.MakeHeader(buffer);

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    Console.Write(msg);
                    return true;
                }
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"수신 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        //void 
    }
}

using Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocket
{
    static class Const
    {
        public const int REQUEST = 100;
        public const int SENDING = 200;
    }

    internal class ClientHandle
    {
        Socket host;

        string fileName;
        int fileSize;

        public ClientHandle(Socket client) 
        {
            host = client;
        }

        public void StartListening()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    byte[] buffer = new byte[4096];
                    int length = host.Receive(buffer);

                    Protocol protocol = new Protocol();
                    protocol.MakeHeader(buffer);

                    switch (protocol.OPCODE)
                    {
                        case Const.REQUEST:
                            int name_length = BitConverter.ToInt32(buffer, protocol.GetSizeHeader());
                            fileName = Encoding.UTF8.GetString(buffer, protocol.GetSizeHeader() + sizeof(int), name_length);
                            fileSize = BitConverter.ToInt32(buffer, protocol.GetSizeHeader() + sizeof(int) + name_length);

                            byte[] response = protocol.TransmitFileResponse(fileName, fileSize);
                            host.Send(response);
                            break;
                        case Const.SENDING:
                            string filePath = Path.Combine(@"..\..\..\..\..\ReceivedFile", fileName);
                            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                //int totalBytesReceived = 0;
                                byte[] fileBuffer = new byte[4096];

                                int bytesToRead = (int)Math.Min(fileBuffer.Length, fileSize) - protocol.GetSizeHeader();
                                Array.Copy(buffer, protocol.GetSizeHeader(), fileBuffer, 0, bytesToRead);

                                if (bytesToRead <= 0)
                                {
                                    Console.WriteLine("파일 수신 중 연결이 끊겼습니다.");
                                    return 102;
                                }

                                fs.Position = fs.Length;
                                fs.Write(fileBuffer, 0, bytesToRead);
                                //totalBytesReceived += bytesToRead;
                            }

                            break;
                        default:
                            Console.WriteLine("미구현 OPCODE");
                            Thread.Sleep(1000);
                            break;
                    }
                }
            });
        }

        public int ReceiveFile()
        {
            try
            {
                if (host.Connected)
                {


                    // 2. 파일 이름 길이 수신 (4바이트 - int형)
                    byte[] fileNameLengthBuffer = new byte[4];
                    int bytesRead = host.Receive(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length, SocketFlags.None);
                    if (bytesRead <= 0)
                    {
                        Console.WriteLine("파일 이름 길이를 수신하는 중 연결이 끊겼습니다.");
                        return 101;
                    }
                    if (!BitConverter.IsLittleEndian) Array.Reverse(fileNameLengthBuffer);
                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBuffer, 0);

                    // 3. 파일 이름 수신
                    byte[] fileNameBuffer = new byte[fileNameLength];
                    bytesRead = host.Receive(fileNameBuffer, 0, fileNameBuffer.Length, SocketFlags.None);
                    if (bytesRead <= 0)
                    {
                        Console.WriteLine("파일 이름을 수신하는 중 연결이 끊겼습니다.");
                        return 101;
                    }
                    //if (BitConverter.IsLittleEndian) Array.Reverse(fileNameLengthBuffer);

                    string fileName = Encoding.UTF8.GetString(fileNameBuffer);
                    Console.WriteLine($"수신할 파일 이름: {fileName}");

                    // 1. 파일 크기 수신 (8바이트 - long형)
                    byte[] fileSizeBuffer = new byte[8];
                    bytesRead = host.Receive(fileSizeBuffer, 0, fileSizeBuffer.Length, SocketFlags.None);
                    if (bytesRead <= 0)
                    {
                        Console.WriteLine("파일 크기를 수신하는 중 연결이 끊겼습니다.");
                        return 102;
                    }
                    if (!BitConverter.IsLittleEndian) Array.Reverse(fileSizeBuffer);
                    long fileSize = BitConverter.ToInt64(fileSizeBuffer, 0);
                    Console.WriteLine($"수신할 파일 크기: {fileSize} 바이트");

                    // 4. 파일 데이터 수신
                    string filePath = Path.Combine(@"..\..\..\..\ReceivedFile", fileName); // 저장 경로 설정
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        long totalBytesReceived = 0;
                        byte[] fileBuffer = new byte[1024];

                        while (totalBytesReceived < fileSize)
                        {
                            int bytesToRead = (int)Math.Min(fileBuffer.Length, fileSize - totalBytesReceived);
                            int bytesReceived = host.Receive(fileBuffer, 0, bytesToRead, SocketFlags.None);

                            if (bytesReceived <= 0)
                            {
                                Console.WriteLine("파일 수신 중 연결이 끊겼습니다.");
                                return 102;
                            }

                            fs.Write(fileBuffer, 0, bytesReceived);

                            //count += bytesToRead;
                            //Console.WriteLine(Encoding.UTF8.GetString(fileBuffer)+ "[count]"+ count);
                            totalBytesReceived += bytesReceived;
                        }

                    }

                    Console.WriteLine($"파일 {fileName} 수신 완료.");
                    return 100;
                }

                return 300;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReceiveFile 오류: {ex.Message}");
                return 301;
            }
        }
    }
}

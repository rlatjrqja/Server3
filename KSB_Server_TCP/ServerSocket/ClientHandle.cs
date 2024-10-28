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
                            FileStream fileStream = new FileStream(@"..\..\..\..\ReceivedFile\Test.txt", FileMode.Open, FileAccess.Read);
                            int receiveSize = 0;
                            while(receiveSize < fileSize)
                            {
                                if (length == 0) break;

                                // 받은 데이터를 파일에 씁니다.
                                fileStream.Write(buffer, 0, length);
                                receiveSize += length;
                            }
                            fileStream.Close();
                            break;
                        default:
                            Console.WriteLine("미구현");
                            break;
                    }
                }
            });
        }
    }
}

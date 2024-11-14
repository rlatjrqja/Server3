using Protocols;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Server_main
{
    public class ClientHandle
    {
        public Socket host;
        Protocol1_File pf;

        public ClientHandle(Socket client)
        {
            host = client;

            Task.Run(() =>
            {
                while (true)
                {
                    byte[] packet = StartListening();
                    Header header = new Header();
                    header.MakeHeader(packet);

                    switch(header.OPCODE)
                    {
                        case Const.CONNECT_REQUEST:
                            ConnectionRequestRecv();
                            break;
                        case Const.FILE_REQUEST:
                            SendFileRequestRecv(header);
                            break;
                        case Const.SENDING:
                            FileReceied(header);
                            break;
                    }
                }
            });
        }

        public byte[] StartListening()
        {
            byte[] buffer = new byte[4096];
            int length = host.Receive(buffer);
            byte[] lawData = new byte[length];
            Array.Copy(buffer, lawData, length);

            return lawData;
        }

        void ConnectionRequestRecv()
        {
            foreach(var handle in RootServer.instance.users)
            {
                if(handle == this) return;
            }
            
            RootServer.instance.users.Add(this);
        }

        void SendFileRequestRecv(Header header)
        {
            byte[] stream = header.BODY;
            int fileName_length = BitConverter.ToInt32(stream, 0);
            string fileName = Encoding.UTF8.GetString(stream, sizeof(int), fileName_length);
            int fileSize = BitConverter.ToInt32(stream, sizeof(int) + fileName_length);

            byte[] response = Protocol1_File.TransmitFileResponse(fileName, fileSize);
            byte[] data = Header.MakePacket(header.proto_VER, 100, 0, response.Length, 0, response);

            host.Send(data);

            // 임시
            pf = new Protocol1_File();
            pf.file_size = fileSize;
            pf.name = fileName;
            pf.name_legth = fileName_length;
        }

        void FileReceied(Header header)
        {
            byte[] stream = header.BODY;
            /*int fileName_length = BitConverter.ToInt32(stream, 0);
            string fileName = Encoding.UTF8.GetString(stream, sizeof(int), fileName_length);
            string filePath = Path.Combine(@"..\..\..\..\..\ReceivedFile", fileName);*/

            int fileName_length = pf.name_legth;
            string fileName = pf.name;
            string filePath = Path.Combine(@"..\..\..\..\..\ReceivedFile", fileName);

            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                byte[] fileBuffer = new byte[4096];

                int bytesToRead = (int)Math.Min(fileBuffer.Length, header.LENGTH);

                Console.WriteLine($"[Receive] ({header.SEQ_NO})\tHeader:{13}Byte + Body:{bytesToRead}Byte");

                Array.Copy(stream, 0, fileBuffer, 0, bytesToRead);

                if (bytesToRead <= 0)
                {
                    Console.WriteLine("파일 수신 중 연결이 끊겼습니다.");
                    //return 201;
                }

                fs.Position = fs.Length;
                fs.Write(fileBuffer, 0, bytesToRead);
            }
        }
    }
}

using Protocols;
using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Server_main
{
    public class ClientHandle
    {
        public Socket host;
        List<Protocol1_File_Log> log_protocol1 = new List<Protocol1_File_Log>();
        //Protocol1_File_Log pf;

        public ClientHandle(Socket client)
        {
            host = client;

            Task.Run(() =>
            {
                while (true)
                {
                    /// 받은 데이터를 헤더(13바이트), 내용물로 분리
                    byte[] packet = StartListening();
                    Header header = new Header();
                    header.MakeHeader(packet);

                    switch(header.OPCODE)
                    {
                        case Const.CONNECT_REQUEST:
                            ConnectionRequestRecv(); /// 접속 요청
                            break;
                        case Const.FILE_REQUEST:
                            SendFileRequestRecv(header); /// 파일 좀 받아줘 (업로드)
                            break;
                        case Const.SENDING:
                            FileReceied(header); /// 파일 보내는 중
                            break;
                    }
                }
            });
        }

        public byte[] StartListening()
        {
            /// 최대 4096 바이트를 받음, 바이트 수 딱 맞게 줄여서 반환
            byte[] buffer = new byte[4096];
            int length = host.Receive(buffer);
            byte[] lawData = new byte[length];
            Array.Copy(buffer, lawData, length);

            return lawData;
        }

        void ConnectionRequestRecv()
        {
            /// 이미 접속한 유저면 접속 요청 무시
            foreach(var handle in RootServer.instance.users)
            {
                if (handle == this)
                {
                    byte[] body = Encoding.UTF8.GetBytes("001 reject");
                    byte[] data = Header.MakePacket(0, 001, 0, body.Length, 0, body);
                    host.Send(data);
                    return;
                }
            }

            {
                RootServer.instance.users.Add(this);
                byte[] body = Encoding.UTF8.GetBytes("000 OK");
                byte[] data = Header.MakePacket(0, 000, 0, body.Length, 0, body);
                host.Send(data);
            }
        }

        void SendFileRequestRecv(Header header)
        {
            /// 파일 내용물에도 헤더가 있음, 이름 및 크기
            byte[] stream = header.BODY;
            int fileName_length = BitConverter.ToInt32(stream, 0);
            string fileName = Encoding.UTF8.GetString(stream, sizeof(int), fileName_length);
            int fileSize = BitConverter.ToInt32(stream, sizeof(int) + fileName_length);

            /// 파일 이름과 크기가 적절한지
            byte[] response = Protocol1_File.TransmitFileResponse(fileName, fileSize);

            /// 
            byte[] data = Header.MakePacket(header.proto_VER, header.OPCODE, 0, response.Length, 0, response);

            host.Send(data);

            // 임시
            log_protocol1.Add(new Protocol1_File_Log(fileSize, fileName, fileName_length));
        }

        void FileReceied(Header header)
        {
            byte[] stream = header.BODY;
            /*int fileName_length = BitConverter.ToInt32(stream, 0);
            string fileName = Encoding.UTF8.GetString(stream, sizeof(int), fileName_length);
            string filePath = Path.Combine(@"..\..\..\..\..\ReceivedFile", fileName);*/

            Protocol1_File_Log pf = log_protocol1.Last();
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

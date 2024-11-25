using Protocols;
using Integrity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ServerSocket
{
    class StateMachine
    {
        public enum State { Waiting, Listening, Recieving }
        public State state { get; private set; }

        public StateMachine() { state = State.Waiting; }
        public State GetState() { return state; }
        public void SetState(State state) {  this.state = state; }
    }

    public class ClientHandle
    {
        public Socket host;
        StateMachine SM = new StateMachine(); /// 일단 구색은 갖췄는데 지금은 사용 안함
        List<Protocol1_File_Log> log_protocol1 = new List<Protocol1_File_Log>();

        public ClientHandle(Socket client)
        {
            host = client;

            Task.Run(() =>
            {
                SM.SetState(StateMachine.State.Listening);
                while (true)
                {
                    /// 받은 데이터를 헤더(16바이트)와 내용물로 분리. 내용물에도 정보는 들어있음
                    byte[] packet = StartListening();
                    Header header = new Header();
                    header.MakeHeader(packet);

                    switch (header.OPCODE)
                    {
                        case Const.CONNECT_REQUEST:
                            ConnectionRequestRecv(); /// 접속 요청
                            break;
                        case Const.FILE_REQUEST:
                            FileUploadRequestRecv(header); /// 파일 좀 받아줘 (업로드)
                            break;
                        case Const.SENDING:
                            FileReceied(header); /// 파일 보내는 중
                            break;
                        case Const.SENDLAST:
                             FileReceied(header);
                            // 방법1. 클라에서 서버로 파일 잘 갔는지 확인
                            // 방법2. 서버에서 클라로 원본 파일 이거 맞는지 요청
                            CheckIntegrity(header); /// 무결성 검사하고 결과 전송 (실패면 재전송 요청)
                            break;
                        case Const.CHECK_PACKET:
                            //CheckIntegrity(header); /// 무결성 검사하고 결과 전송 (실패면 재전송 요청)
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
            foreach (var handle in RootServer.instance.users)
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

        void FileUploadRequestRecv(Header header)
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
            byte[] encryptedStream = header.BODY; // 암호화된 데이터

            // AES 복호화: 클라이언트에서 사용한 Key와 IV를 서버에서 동일하게 설정해야 함
            AES aes = new AES();
            byte[] decryptedStream = aes.DecryptData(encryptedStream);

            Protocol1_File_Log pf = log_protocol1.Last();
            int fileName_length = pf.name_legth;
            string fileName = pf.name;
            string filePath = @"..\..\..\..\..\ReceivedFile" +  fileName;
            string fullPath = Path.GetFullPath(filePath);

            using (FileStream fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                int bytesToRead = (int)Math.Min(decryptedStream.Length, header.LENGTH);

                Console.WriteLine($"[Receive] ({header.SEQ_NO})\tHeader:{16}Byte + Body:{bytesToRead}Byte");

                if (bytesToRead <= 0)
                {
                    Console.WriteLine("파일 수신 중 연결이 끊겼습니다.");
                    return;
                }

                // Append decrypted data to the file
                fs.Position = fs.Length;
                fs.Write(decryptedStream, 0, bytesToRead);
            }
        }

        private void CheckIntegrity(Header header)
        {
            byte[] encryptedStream = header.BODY;
            AES aes = new AES();
            byte[] decryptedStream = aes.DecryptData(encryptedStream);

            Protocol1_File_Log pf = log_protocol1.Last();
            int fileName_length = pf.name_legth;
            string fileName = pf.name;
            string filePath = Path.Combine(@"..\..\..\..\..\ReceivedFile", fileName);

            byte[] binary = Protocol1_File.FileToBinary(filePath, fileName);
            bool isFullRecv = MySHA256.CompareHashes(binary, decryptedStream);
            if(isFullRecv) // 파일 전송 전 후의 해시가 같음 (정상전송)
            {
                byte[] result = Encoding.UTF8.GetBytes("File upload success");
                byte[] data = Header.MakePacket(0, 300, 0, result.Length, 0, result);
                host.Send(data);
            }
            else // 해시가 다름. 재전송 요청
            {
                byte[] result = Encoding.UTF8.GetBytes("File upload fail");
                byte[] data = Header.MakePacket(0, 301, 0, result.Length, 0, result);
                host.Send(data);
            }
        }
    }
}

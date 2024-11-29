using Protocols;
using Integrity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ServerSocket
{
    class StateMachine
    {
        public enum AuthenticState { Guest, Member }
        public AuthenticState State { get; private set; }

        public StateMachine() { State = AuthenticState.Guest; }
        public AuthenticState GetState() { return State; }
        public void SetState(AuthenticState state) {  this.State = state; }
    }

    /// <summary>
    /// 서버 -> 클라이언트로 요청을 보내지 않음.
    /// 서버의 기본적인 의의는 받은 요청에 대해 응답하는 것으로 설계하였음.
    /// </summary>
    public class ClientHandle
    {
        public Socket host;

        List<Protocol1_File_Log> log_protocol1 = new();
        string server_dir = @"..\..\..\..\..\KSB_Server_TCP"; //ReceivedFile

        StateMachine SM = new(); /// 일단 구색은 갖췄는데 아직은 사용 안함

        public ClientHandle(Socket client)
        {
            host = client;

            Task.Run(() =>
            {
                // SM.SetState(StateMachine.AuthenticState.Listening);
                while (true)
                {
                    /// 받은 데이터를 분리. 패킷 정보를 담은 헤더와 정보 및 실제 데이터를 담은 바디로 구성
                    byte[] packet = StartListening();
                    Header header = new();
                    header.DisassemblePacket(packet);

                    switch (header.OPCODE)
                    {
                        case Const.CONNECT_REQUEST:
                            /// 접속 요청 받음
                            ConnectionRequest(header); 
                            break;
                        case Const.CREATE_ACCOUNT:
                            CreateAccountRequest(header);
                            break;
                        case Const.LOGIN:
                            TryLoginRequest(header);
                            break;
                        case Const.FILE_REQUEST:
                            /// 파일 업로드 요청 받음
                            FileUploadRequest(header); 
                            break;
                        case Const.SENDING:
                            /// 파일 받는 중
                            FileReceive(header); 
                            break;
                        case Const.SENDLAST:
                            /// 파일 받기 끝
                            FileReceive(header);
                            FileReceiveEnd();
                            break;
                        case Const.CHECK_PACKET:
                            /// 방법1. 클라에서 서버로 파일 잘 갔는지 확인
                            /// 방법2. 서버에서 클라로 원본 파일 이거 맞는지 요청 -> 현재 사용 중인 방법
                            /// 무결성 검사하고 결과 전송 (실패면 재전송 요청)
                            CheckIntegrity(header);
                            break;
                        default:
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

        void ConnectionRequest(Header header)
        {
            /// 이미 접속한 유저면 접속 요청 무시
            foreach (var handle in RootServer.instance.users)
            {
                if (handle == this)
                {
                    byte[] response = Encoding.UTF8.GetBytes("001 reject");
                    byte[] data = Header.AssemblePacket(0, Const.CONNECT_REJECT, 0, response.Length, 0, response);
                    host.Send(data);
                    return;
                }
            }

            /// 신규 접속
            {
                RootServer.instance.users.Add(this);
                byte[] response = Encoding.UTF8.GetBytes("000 OK");
                byte[] data = Header.AssemblePacket(0, Const.CONNECT_REQUEST, 0, response.Length, 0, response);
                host.Send(data);
            }
        }

        void CreateAccountRequest(Header header)
        {
            /// 회원 목록 여기에 추가
            byte[] stream = header.BODY;
            string info = Encoding.UTF8.GetString(stream);

            string filePath = server_dir + @"\database\userDB.json";
            string fullPath = Path.GetFullPath(filePath);
            string database = Protocol3_Json.JsonFileToString(fullPath);
            string updatedata = Protocol3_Json.AddDataIntoJson(database, info);

            if (updatedata != null)
            {
                Protocol3_Json.StringToJsonFile(fullPath, updatedata);
                byte[] response = Encoding.UTF8.GetBytes("Login success");
                byte[] data = Header.AssemblePacket(0, Const.CREATE_ACCOUNT, 0, response.Length, 0, response);
                host.Send(data);
            }
            else
            {
                byte[] response = Encoding.UTF8.GetBytes("Login fail");
                byte[] data = Header.AssemblePacket(0, Const.CREATE_FAIL, 0, response.Length, 0, response);
                host.Send(data);
            }
        }

        void TryLoginRequest(Header header)
        {
            byte[] stream = header.BODY;
            string info = Encoding.UTF8.GetString(stream);

            string filePath = server_dir + @"\database\userDB.json";
            string fullPath = Path.GetFullPath(filePath);
            string database = Protocol3_Json.JsonFileToString(fullPath);
            bool isvalid = Protocol3_Json.IsJsonIncludeData(database, info);

            if (isvalid)
            {
                byte[] response = Encoding.UTF8.GetBytes("Login success");
                byte[] data = Header.AssemblePacket(0, Const.LOGIN, 0, response.Length, 0, response);
                host.Send(data);
            }
            else
            {
                byte[] response = Encoding.UTF8.GetBytes("Login fail");
                byte[] data = Header.AssemblePacket(0, Const.LOGIN_FAIL, 0, response.Length, 0, response);
                host.Send(data);
            }
        }

        void FileUploadRequest(Header header)
        {
            if(SM.State == StateMachine.AuthenticState.Member)
            {
                /// 파일 내용물에도 헤더가 있음, 이름길이 - 이름 - 파일크기 순
                byte[] stream = header.BODY;
                int fileName_length = BitConverter.ToInt32(stream, 0);
                string fileName = Encoding.UTF8.GetString(stream, sizeof(int), fileName_length);
                int fileSize = BitConverter.ToInt32(stream, sizeof(int) + fileName_length);

                /// 파일 이름과 크기가 적절한지
                int retult = Protocol1_File.TransmitFileResponse(fileName, fileSize);
                switch(retult)
                {
                    case 100:
                        byte[] response1 = Encoding.UTF8.GetBytes("100_OK");
                        byte[] data1 = Header.AssemblePacket(0, Const.FILE_REQUEST, 0, response1.Length, 0, response1);
                        host.Send(data1);
                        break;
                    case 101:
                        byte[] response2 = Encoding.UTF8.GetBytes("filename is too long"); 
                        byte[] data2 = Header.AssemblePacket(0, Const.FILE_REJECT, 0, response2.Length, 0, response2);
                        host.Send(data2);
                        break;
                    case 102:
                        byte[] response3 = Encoding.UTF8.GetBytes("file is too big");
                        byte[] data3 = Header.AssemblePacket(0, Const.FILE_REJECT, 0, response3.Length, 0, response3);
                        host.Send(data3);
                        break;
                }

                /// 받은 요청은 로그에 추가
                log_protocol1.Add(new Protocol1_File_Log(fileSize, fileName, fileName_length));
            }
            else
            {
                byte[] response = Encoding.UTF8.GetBytes("You have to login first.");
                byte[] data = Header.AssemblePacket(0, Const.FILE_REJECT, 0, response.Length, 0, response);
                host.Send(data);
            }
        }

        void FileReceive(Header header)
        {
            /// 받은 파일 바이너리 (암호화 전)
            byte[] encryptedData = header.BODY;

            /// MyAES 복호화. 라이브러리에 고정값 사용중 (보안성 낮음)
            MyAES aes = new MyAES();
            byte[] decryptedData = aes.DecryptData(encryptedData);

            /// 경로 탐색
            Protocol1_File_Log pf = log_protocol1.Last();
            string fileName = pf.name;
            string filePath = server_dir+ @"\files" + fileName;
            string fullPath = Path.GetFullPath(filePath);

            using (FileStream fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                int bytesToRead = (int)Math.Min(decryptedData.Length, header.LENGTH);

                Console.WriteLine($"[Receive] ({header.SEQ_NO})\tHeader:{16}Byte + Body:{bytesToRead}Byte");

                if (bytesToRead <= 0)
                {
                    Console.WriteLine("파일 수신 중 연결이 끊겼습니다.");
                    return;
                }

                // Append decrypted data to the file
                fs.Position = fs.Length;
                fs.Write(decryptedData, 0, bytesToRead);

                if(header.OPCODE == Const.SENDLAST) fs.Close();
            }
        }

        private void FileReceiveEnd()
        {
            /// CRC 부분 재전송 할거면 여기서 처리
            /// CRC 1인 패킷들 리스트로 모아서 전송

            byte[] data = Header.AssemblePacket(0, Const.SENDLAST, 0, 1, 0, new byte[1]);
            host.Send(data);
        }

        private void CheckIntegrity(Header header)
        {
            /// 해시도 암호화를 해야할까?
            /*byte[] encryptedData = header.BODY;
            MyAES aes = new MyAES();
            byte[] decryptedData = aes.DecryptData(encryptedData);*/

            /// 경로 탐색
            Protocol1_File_Log pf = log_protocol1.Last();
            string fileName = pf.name;
            string filePath = server_dir + fileName;
            string fullPath = Path.GetFullPath(filePath);

            /// 전송 받은 파일 해시값 계산
            byte[] binary = Protocol1_File.FileToBinary(fullPath);
            byte[] hash = MySHA256.CreateHash(binary);
            bool isFullRecv = MySHA256.CompareHashes(hash, header.BODY);

            /// 파일 전송 전 후의 해시가 같음 (정상전송)
            if (isFullRecv)
            {
                byte[] result = Encoding.UTF8.GetBytes("File upload success");
                byte[] data = Header.AssemblePacket(0, Const.CHECK_PACKET, 0, result.Length, 0, result);
                host.Send(data);
            }
            /// 해시가 다름. 재전송 요청
            else
            {
                byte[] result = Encoding.UTF8.GetBytes("File upload fail");
                byte[] data = Header.AssemblePacket(0, Const.CHECK_DIFF, 0, result.Length, 0, result);
                host.Send(data);
            }
        }
    }
}

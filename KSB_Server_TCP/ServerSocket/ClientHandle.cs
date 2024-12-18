﻿using Protocols;
using Integrity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static ServerSocket.StateMachine;
using System.Reflection.PortableExecutable;

namespace ServerSocket
{
    class StateMachine
    {
        public enum AuthenticState { Guest, Member }
        public AuthenticState State { get; private set; }

        public StateMachine() { State = AuthenticState.Guest; }
        public void SetState(AuthenticState state) {  this.State = state; }
    }

    /// <summary>
    /// 서버 -> 클라이언트로 요청을 보내지 않음.
    /// 서버의 기본적인 의의는 받은 요청에 대해 응답하는 것으로 설계하였음.
    /// </summary>
    public class ClientHandle
    {
        public Socket host;
        string name = "Unknown";

        List<Protocol1_File_Log> log_protocol1 = new();
        string server_dir = @"..\..\..\..\..\KSB_Server_TCP";

        StateMachine SM = new();

        public ClientHandle(Socket client)
        {
            host = client;

            Task.Run(() =>
            {
                while (host.Connected)
                {
                    /// 받은 데이터를 분리. 패킷 정보를 담은 헤더와 정보 및 실제 데이터를 담은 바디로 구성
                    byte[] packet = new byte[4096];
                    try { host.Receive(packet); }
                    catch { 
                        host.Disconnect(false);
                        RootServer.instance.users.Remove(this);
                        return;
                    }
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
                        case Const.FILE_UPLOAD:
                            /// 파일 업로드 요청 받음
                            FileUploadRequest(header); 
                            break;
                        case Const.TEXT_SEND:
                            TextReceive(header);
                            break;
                        case Const.FILE_VIEWLIST:
                            FileListRequest(header);
                            break;
                        case Const.FILE_DOWNLOAD:
                            FileDownLoad(header);
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
                        case Const.GET_OFF:
                            CloseHandle(header);
                            break;
                        default:
                            break;
                    }
                }
            });
        }

        /// <summary>
        /// 굳이 필요없는 내용이라 생략
        /// 삭제는 보류
        /// </summary>
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
            string key = Protocol3_Json.IsJsonIncludeData(database, info);
             
            if (key != null)
            {
                name = key;
                byte[] response = Encoding.UTF8.GetBytes("Login success");
                byte[] data = Header.AssemblePacket(0, Const.LOGIN, 0, response.Length, 0, response);
                SM.SetState(AuthenticState.Member);
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
            if(SM.State == AuthenticState.Member)
            {
                /// 파일 내용물에도 헤더가 있음, 이름길이 - 이름 - 파일크기 순
                byte[] stream = header.BODY;
                int fileName_length = BitConverter.ToInt32(stream, 0);
                string fileName = Encoding.UTF8.GetString(stream, sizeof(int), fileName_length);
                int fileSize = BitConverter.ToInt32(stream, sizeof(int) + fileName_length);

                /// 파일 이름과 크기가 적절한지
                int retult = Protocol1_File.TransmitFileResponse(fileName, fileSize, server_dir + @"\files\");
                switch(retult)
                {
                    case 100:
                        byte[] response1 = Encoding.UTF8.GetBytes("100_OK");
                        byte[] data1 = Header.AssemblePacket(0, Const.FILE_UPLOAD, 0, response1.Length, 0, response1);
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
                    case 103:
                        int i = 1;
                        while (i<100)
                        {
                            string[] splited = fileName.Split('.');
                            string chagedName = splited[0]+ $" ({i})." + splited[1];
                            if (File.Exists(server_dir + @"\files\" + chagedName))
                            {
                                i++;
                            }
                            else
                            {
                                byte[] response4 = Encoding.UTF8.GetBytes(chagedName);
                                byte[] data4 = Header.AssemblePacket(0, Const.FILE_RENAME, 0, response4.Length, 0, response4);
                                host.Send(data4);
                                break;
                            }
                        }
                        
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

        private void TextReceive(Header header)
        {
            byte[] encryptedData = header.BODY;
            MyAES aes = new MyAES();
            byte[] decryptedData = aes.DecryptData(encryptedData);
            string message = Encoding.UTF8.GetString(decryptedData);
            Console.WriteLine($"{name}: { message }");

            DateTime time = new();
            byte[] result = BitConverter.GetBytes(time.Second);
            byte[] data = Header.AssemblePacket(0, Const.TEXT_SEND, 0, result.Length, 0, result);
            host.Send(data);
        }

        private void FileListRequest(Header header)
        {
            List<string> fileNames = Protocol1_File.LoadFileList(server_dir + @"\files\");

            string file_list = "";
            for (int i = 0; i < fileNames.Count; i++)
            {
                file_list += $"{i + 1}. {Path.GetFileName(fileNames[i])}\n";
            }

            byte[] response = Encoding.UTF8.GetBytes(file_list);
            byte[] data = Header.AssemblePacket(0, Const.FILE_VIEWLIST, 0, response.Length, 0, response);
            host.Send(data);
        }

        private void FileDownLoad(Header header)
        {
            MyAES aes = new MyAES();
            string name = Encoding.UTF8.GetString(header.BODY);
            byte[] binary = Protocol1_File.FileToBinary(server_dir + @"\files\", name);
            List<byte[]> packets = Protocol1_File.TransmitFile(binary);
            for (int i = 0; i < packets.Count; i++)
            {
                byte[] encryptedSegment = aes.EncryptData(packets[i]);

                if (i != packets.Count - 1)
                {
                    // 암호화된 패킷 전송
                    host.Send(Header.AssemblePacket(1, Const.SENDING, i, encryptedSegment.Length, 0, encryptedSegment));
                    Console.WriteLine($"[Send] {encryptedSegment.Length} Byte (Packet {i + 1}/{packets.Count})");
                }
                else
                {
                    // 암호화된 패킷 전송
                    host.Send(Header.AssemblePacket(1, Const.SENDLAST, i, encryptedSegment.Length, 0, encryptedSegment));
                    Console.WriteLine($"[Send] {encryptedSegment.Length} Byte (Packet {i + 1}/{packets.Count})");
                }
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
            string filePath = server_dir+ @"\files\" + fileName;
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
            string filePath = server_dir+ @"\files\" + fileName;
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


        private void CloseHandle(Header header)
        {
            byte[] result = Encoding.UTF8.GetBytes("500_OK");
            byte[] data = Header.AssemblePacket(0, Const.CHECK_DIFF, 0, result.Length, 0, result);
            host.Send(data);
            host.Disconnect(true);
            RootServer.instance.users.Remove(this);
        }
    }
}

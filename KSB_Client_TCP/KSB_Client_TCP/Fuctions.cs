﻿using Integrity;
using Login;
using Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KSB_Client_TCP
{
    internal class Fuctions
    {
        /// <summary>
        /// 서버에 요청 보낸 뒤 응답을 대기 하는 용도
        /// </summary>
        public static Header WaitForServerResponse(Socket host)
        {
            byte[] buffer = new byte[4096];
            int bytesReceived = host.Receive(buffer);
            if (bytesReceived > 0)
            {
                Header header = new Header();
                header.DisassemblePacket(buffer);
                return header;
            }
            return null;
        }

        /// <summary>
        /// OPCODE가 기대하는 값을 받았는가
        /// </summary>
        public static bool CheckOPCODE(Header hd, int opcode, string correctMSG, string failMSG)
        {
            if (hd.OPCODE == opcode)
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

        /// <summary>
        /// OPCODE 000
        /// 추후 클라이언트에서 할 수 있는게 뭔지 메뉴 불러오기로 변경
        /// </summary>
        public static bool InitializeConnection(Socket host)
        {
            byte[] connection = Header.AssemblePacket(0, Const.CONNECT_REQUEST, 0, 1, 0, new byte[1]);
            host.Send(connection);

            Header response_connect = WaitForServerResponse(host);
            return CheckOPCODE(response_connect, Const.CONNECT_REQUEST, "서버 접속 성공", "서버 접속 실패");
        }

        /// <summary>
        /// OPCODE 010
        /// </summary>
        public static void CreateAccount(Socket host)
        {
            string info = UserInfo.CreateID();
            byte[] buffer = Encoding.UTF8.GetBytes(info);
            byte[] request = Header.AssemblePacket(0, Const.CREATE_ACCOUNT, 0, buffer.Length, 0, buffer);
            host.Send(request);
            
            Header response_create = WaitForServerResponse(host);
            if (CheckOPCODE(response_create, Const.CREATE_ACCOUNT, "회원가입 성공", "가입 실패"))
            {

            }
            else
            {
                string reason = Encoding.UTF8.GetString(response_create.BODY);
                Console.WriteLine(reason);
            }
        }

        /// <summary>
        /// OPCODE 020
        /// </summary>
        public static void TryLogin(Socket host)
        {
            string info = UserInfo.CreateID();
            byte[] buffer = Encoding.UTF8.GetBytes(info);
            byte[] request = Header.AssemblePacket(0, Const.LOGIN, 0, buffer.Length, 0, buffer);
            host.Send(request);

            Header response_login = WaitForServerResponse(host);
            if (CheckOPCODE(response_login, Const.LOGIN, "로그인 성공", "로그인 실패"))
            {

            }
            else
            {
                string reason = Encoding.UTF8.GetString(response_login.BODY);
                Console.WriteLine(reason);
            }
        }

        /// <summary>
        /// OPCODE 100
        /// </summary>
        public static void FileTransferRequest(Socket host, string rootDir, string name)
        {
            // 파일 전송 요청
            byte[] binary = Protocol1_File.FileToBinary(rootDir, name);
            Console.WriteLine($"파일 크기: {binary.Length} 바이트");

            byte[] request = Protocol1_File.TransmitFileRequest(name.Length, name, binary.Length);
            byte[] data = Header.AssemblePacket(1, Const.FILE_REQUEST, 0, request.Length, 0, request);
            host.Send(data);

            Header response_file = WaitForServerResponse(host);
            if (CheckOPCODE(response_file, Const.FILE_REQUEST, "파일 전송 가능", "파일 전송 불가"))
            {
                FileTransfer(host, rootDir, name);
            }
            else
            {
                string reason = Encoding.UTF8.GetString(response_file.BODY);
                Console.WriteLine(reason);
                return;
            }

            Header response_end = WaitForServerResponse(host);
            if (CheckOPCODE(response_end, Const.SENDLAST, "마지막 패킷 수신 알림", "수신 중 이상 발생"))
            {
                // 해시 전송
                FileCheckRequest(host, rootDir, name);
            }
            else
            {
                string reason = Encoding.UTF8.GetString(response_end.BODY);
                Console.WriteLine(reason);
                //Console.WriteLine("여기에 재전송 구현");
            }
        }

        /// <summary>
        /// OPCODE 110
        /// </summary>
        public static void TextTransfer(Socket host)
        {
            byte[] text = Protocol2_Plain.GetMessage();

            MyAES aes = new MyAES();
            byte[] encryptedSegment = aes.EncryptData(text);

            byte[] data = Header.AssemblePacket(2, Const.TEXT_SEND, 0, encryptedSegment.Length, 0, encryptedSegment);
            host.Send(data);

            Header response_text = WaitForServerResponse(host);
            string response_msg = Encoding.UTF8.GetString(response_text.BODY);
            if (CheckOPCODE(response_text, Const.TEXT_SEND, response_msg, response_msg))
            {

            }
        }

        /// <summary>
        /// OPCODE 200
        /// </summary>
        public static void FileTransfer(Socket host, string rootDir, string name)
        {
            MyAES aes = new MyAES();
            byte[] binary = Protocol1_File.FileToBinary(rootDir, name);
            List<byte[]> packets = Protocol1_File.TransmitFile(binary);
            for (int i = 0; i < packets.Count; i++)
            {
                byte[] encryptedSegment = aes.EncryptData(packets[i]);

                if (packets[i] != packets.Last())
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

        /// <summary>
        /// OPCODE 300
        /// </summary>
        public static void FileCheckRequest(Socket host, string rootDir, string name)
        {
            byte[] binary = Protocol1_File.FileToBinary(rootDir, name);
            byte[] hash = MySHA256.CreateHash(binary);
            byte[] integrity = Header.AssemblePacket(0, Const.CHECK_PACKET, 0, hash.Length, 0, hash);
            host.Send(integrity);
        }

        /// <summary>
        /// OPCODE 500
        /// </summary>
        public static void Disconnect(Socket host)
        {
            byte[] disconnectPacket = Header.AssemblePacket(0, Const.GET_OFF, 0, 1, 0, new byte[1]);
            host.Send(disconnectPacket);
            host.Close();
            Console.WriteLine("서버와의 연결을 종료했습니다.");
        }
    }
}

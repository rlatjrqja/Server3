using Integrity;
using Login;
using Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KSB_Client_TCP
{
    internal class Fuctions
    {
        /// <summary>
        /// OPCODE 010
        /// </summary>
        public static void CreateAccount(Socket host)
        {
            // byte[] body = Encoding.UTF8.GetBytes("Connection Request"); 로그인 구현으로 비활성
            string info = UserInfo.CreateID();
            byte[] buffer = Protocol3_Json.JsonStringToBytes(info);
            byte[] request = Header.AssemblePacket(0, Const.CREATE_ACCOUNT, 0, buffer.Length, 0, buffer);
            host.Send(request);
            Console.WriteLine($"서버 접속 요청 [Length]:{request.Length}");
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
            byte[] data = Header.AssemblePacket(0, Const.FILE_REQUEST, 0, request.Length, 0, request);
            host.Send(data);
            Console.WriteLine("파일 전송 가능 상태 확인");
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
                    host.Send(Header.AssemblePacket(0, Const.SENDING, i, encryptedSegment.Length, 0, encryptedSegment));
                    Console.WriteLine($"[Send] {encryptedSegment.Length} Byte (Packet {i + 1}/{packets.Count})");
                }
                else
                {
                    // 암호화된 패킷 전송
                    host.Send(Header.AssemblePacket(0, Const.SENDLAST, i, encryptedSegment.Length, 0, encryptedSegment));
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
            /*Header response_ok = WaitForServerResponse(host);
            if (CheckOPCODE(response_ok, Const.CHECK_PACKET, "파일 전송 완료", "파일 전송 실패"))
            {
                Console.WriteLine("END");
            }
            else
            {
                Console.WriteLine("여기에 재전송 구현");
            }*/
            byte[] hash = MySHA256.CreateHash(binary);
            byte[] integrity = Header.AssemblePacket(0, Const.CHECK_PACKET, 0, hash.Length, 0, hash);
            host.Send(integrity);
        }

        /// <summary>
        /// OPCODE 500
        /// </summary>
        public static void Disconnect(Socket host)
        {
            byte[] disconnectPacket = Header.AssemblePacket(0, Const.GET_OFF, 0, 0, 0, new byte[0]);
            host.Send(disconnectPacket);
            host.Close();
            Console.WriteLine("서버와의 연결을 종료했습니다.");
        }
    }
}

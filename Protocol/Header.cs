using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace Protocols
{
    public static class Const
    {
        public const int CONNECT_REQUEST = 000;
        public const int CONNECT_REJECT = 001;
        public const int FILE_REQUEST = 100;
        public const int SENDING = 200;
        public const int SENDLAST = 210;
        public const int CHECK_PACKET = 300;
    }

    /// <summary>
    /// 핸들에서 로우데이터를 넘겨받아 헤더와 데이터를 분리하는 역할
    /// </summary>
    public class Header
    {
        public Byte proto_VER { get; private set; }
        public ushort OPCODE { get; private set; }
        public uint SEQ_NO { get; private set; }
        public uint LENGTH { get; private set; }
        public ushort CRC { get; private set; }
        public Byte[] BODY { get; private set; }

        public int GetHeaderSize()
        {
            return sizeof(Byte) + sizeof(ushort) + sizeof(uint) + sizeof(uint) + sizeof(ushort) + 3;
        }

        public int MakeHeader(byte[] packet)
        {
            // 1바이트 복사
            proto_VER = packet[0];

            // 2바이트 복사 (OPCODE)
            OPCODE = BitConverter.ToUInt16(packet, 1);

            // 4바이트 복사 (SEQ_NO)
            SEQ_NO = BitConverter.ToUInt32(packet, 3);

            // 4바이트 복사 (LENGTH)
            LENGTH = BitConverter.ToUInt32(packet, 7);

            // 2바이트 복사 (CRC)
            CRC = BitConverter.ToUInt16(packet, 11);

            BODY = new Byte[LENGTH];

            Array.Copy(packet, GetHeaderSize(), BODY, 0, LENGTH);

            return sizeof(Byte) + sizeof(ushort) + sizeof(uint) + sizeof(uint) + sizeof(ushort) + (int)LENGTH;
        }

        public static byte[] MakePacket(int ver, int op, int seq, int len, int crc, byte[] data)
        {
            List<byte> packet = new List<byte>();
            packet.Add((Byte)ver);
            packet.AddRange(BitConverter.GetBytes((ushort)op));
            packet.AddRange(BitConverter.GetBytes((uint)seq));
            packet.AddRange(BitConverter.GetBytes((uint)len));
            packet.AddRange(BitConverter.GetBytes((ushort)crc));

            /// 헤더를 20 바이트로 만들기 위한 발버둥
            /// 암호화를 고려해 body 크기를 4080 으로 맞추기 위함
            int count = packet.Count%16;
            while(count != 0 && count != 16)
            {
                packet.Add(0x00);
                count++;
            }
            //packet.AddRange(new Byte[7] { 0x00,0x00,0x00, 0x00, 0x00, 0x00, 0x00 }); 
            packet.AddRange(data);

            return packet.ToArray();
        }
    }
}

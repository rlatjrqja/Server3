using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Protocols
{
    public static class Const
    {
        public const int CONNECT_REQUEST = 000;
        public const int FILE_REQUEST = 100;
        public const int SENDING = 200;
        public const int SENDLAST = 210;
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
        public Byte[] BODY;

        public int MakePacket(byte[] packet)
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

            Array.Copy(packet, 13, BODY, 0, packet.Length - 13);

            return sizeof(Byte) + sizeof(ushort) + sizeof(uint) + sizeof(uint) + sizeof(ushort);
        }
    }
}

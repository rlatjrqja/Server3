using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocols
{
    public class Header
    {
        public Byte proto_VER { get; private set; }
        public ushort OPCODE { get; private set; }
        public uint SEQ_NO { get; private set; }
        public uint LENGTH { get; private set; }
        public ushort CRC { get; private set; }

        public Header()
        {

        }

        public void ByteToHeader(byte[] data)
        {
            // 1바이트 복사
            proto_VER = data[0];

            // 2바이트 복사
            byte[] opcode = new byte[2];
            data.CopyTo(opcode, sizeof(byte));
            OPCODE = BitConverter.ToUInt16(opcode, 0);

            // 4바이트 복사
            byte[] seq = new byte[4];
            data.CopyTo(seq, sizeof(byte) + sizeof(ushort));
            SEQ_NO = BitConverter.ToUInt32(seq, 0);

            byte[] len = new byte[4];
            data.CopyTo(len, sizeof(byte) + sizeof(ushort) + sizeof(uint));
            SEQ_NO = BitConverter.ToUInt32(len, 0);

            byte[] crc = new byte[2];
            data.CopyTo(crc, sizeof(byte) + sizeof(ushort) + sizeof(uint) + sizeof(uint));
            OPCODE = BitConverter.ToUInt16(crc, 0);
        }
    }
}

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

        public void ByteToHeader(byte[] data)
        {
            // 1바이트 복사
            proto_VER = data[0];

            // 2바이트 복사 (OPCODE)
            OPCODE = BitConverter.ToUInt16(data, 1);

            // 4바이트 복사 (SEQ_NO)
            SEQ_NO = BitConverter.ToUInt32(data, 3);

            // 4바이트 복사 (LENGTH)
            LENGTH = BitConverter.ToUInt32(data, 7);

            // 2바이트 복사 (CRC)
            CRC = BitConverter.ToUInt16(data, 11);
        }

        public byte[] HeaderToByte(int ver, int op, int seq, int len, int crc)
        {
            byte[] header = new byte[13];

            header[0] = (byte)ver;
            Array.Copy(BitConverter.GetBytes((ushort)op), 0, header, 1, 2); // 2 bytes for OPCODE
            Array.Copy(BitConverter.GetBytes((uint)seq), 0, header, 3, 4);  // 4 bytes for SEQ_NO
            Array.Copy(BitConverter.GetBytes((uint)len), 0, header, 7, 4);  // 4 bytes for LENGTH
            Array.Copy(BitConverter.GetBytes((ushort)crc), 0, header, 11, 2); // 2 bytes for CRC

            return header;
        }
    }
}

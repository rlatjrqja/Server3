using System.Reflection.Emit;
using System;
using System.Text;
using System.Net.Sockets;

namespace Protocols
{
    public class Protocol
    {
        public Byte proto_VER { get; private set; }
        public ushort OPCODE { get; private set;}
        public uint SEQ_NO { get; private set;}
        public uint LENGTH { get; private set;}
        public ushort CRC { get; private set;}
        Byte[] BODY;

        // 반환시 헤더 크기 반환
        public int MakeHeader(int ver, int op, int seq, int len, int crc) 
        {
            proto_VER = (Byte)ver;
            OPCODE = (ushort)op;
            SEQ_NO = (uint)seq;
            LENGTH = (uint)len;
            CRC = (ushort)crc;

            return GetSizeHeader();
        }
        public int MakeHeader(byte[] headerBuffer)
        {
            // 1바이트 복사
            proto_VER = headerBuffer[0];

            // 2바이트 복사 (OPCODE)
            OPCODE = BitConverter.ToUInt16(headerBuffer, 1);

            // 4바이트 복사 (SEQ_NO)
            SEQ_NO = BitConverter.ToUInt32(headerBuffer, 3);

            // 4바이트 복사 (LENGTH)
            LENGTH = BitConverter.ToUInt32(headerBuffer, 7);

            // 2바이트 복사 (CRC)
            CRC = BitConverter.ToUInt16(headerBuffer, 11);

            return GetSizeHeader();
        }
        public int GetSizeHeader() 
        {
            return sizeof(Byte) + sizeof(ushort) + sizeof(uint) + sizeof(uint) + sizeof(ushort); 
        }

        // 클라 측에서 접속 요청하는 헤더
        public Byte[] StartConnectionRequest() 
        {
            BODY = Encoding.UTF8.GetBytes("Connection Request");
            MakeHeader(0, 000, 0, BODY.Length, 0);

            return PacketToByte();
        }

        // 서버 측에서 접속 승인하는 헤더
        public Byte[] StartConnectionResponse(bool ok) 
        {
            if (ok)
            {
                BODY = Encoding.UTF8.GetBytes("000 OK");
                MakeHeader(0, 000, 0, BODY.Length, 0);
            }
            else
            {
                BODY = Encoding.UTF8.GetBytes("001 reject");
                MakeHeader(0, 001, 0, BODY.Length, 0);
            }

            return PacketToByte();
        }


        public Byte[] TransmitFileRequest(int name_legth, string name, int file_size) 
        {
            List<byte> request = new List<byte>();
            request.AddRange(BitConverter.GetBytes(name_legth));
            request.AddRange(Encoding.UTF8.GetBytes(name));
            request.AddRange(BitConverter.GetBytes(file_size));
            BODY = request.ToArray();

            MakeHeader(0, 100, 0, BODY.Length, 0);

            return PacketToByte();
        }
        public Byte[] TransmitFileResponse(string filename, int size) 
        {
            /// 파일 이름 길이 20글자 제한
            if(filename.Length > 40)
            {
                BODY = Encoding.UTF8.GetBytes("101_filename is too long");
                MakeHeader(0, 101, 0, BODY.Length, 0);
            }

            else if (size > int.MaxValue)
            {
                BODY = Encoding.UTF8.GetBytes("102_file is too big");
                MakeHeader(0, 102, 0, BODY.Length, 0);
            }

            else
            {
                BODY = Encoding.UTF8.GetBytes("100_OK");
                MakeHeader(0, 100, 0, BODY.Length, 0);
            }

            return PacketToByte();
        }

        public List<Byte[]> TransmitFile(byte[] binary)
        {
            List<Byte[]> packets = new List<Byte[]>();
            int headerSize = GetSizeHeader();
            int maxDataSize = 4096 - headerSize;
            int totalPackets = (int)Math.Ceiling((double)binary.Length / maxDataSize);

            for (int seq = 0; seq < totalPackets; seq++)
            {
                int dataStartIndex = seq * maxDataSize;
                int dataLength = Math.Min(maxDataSize, binary.Length - dataStartIndex);
                byte[] packet = new byte[headerSize + dataLength];

                // 실제 데이터 배열을 생성
                if (dataLength < 2000)
                    Console.Write("Debug");
                byte[] dataChunk = new byte[dataLength];
                Array.Copy(binary, dataStartIndex, dataChunk, 0, dataLength);
                BODY = dataChunk;

                // 헤더 생성 (SEQ_NO 등 필요한 정보 포함)
                if (seq != totalPackets - 1) MakeHeader(0, 200, seq, headerSize + dataLength, 0);
                else MakeHeader(0, 210, seq, headerSize + dataLength, 0); // 마지막 패킷

                // 패킷 생성: 헤더와 데이터 병합
                //Array.Copy(PacketToByte(), 0, packet, 0, headerSize); // 헤더 복사
                //Array.Copy(dataChunk, 0, packet, headerSize, dataLength); // 데이터 복사
                Array.Copy(PacketToByte(), packet, packet.Length);

                packets.Add(packet);
            }

            return packets;
        }

        public Byte[] PacketToByte()
        {
            List<byte> packet = new List<byte>();
            packet.Add(proto_VER);
            packet.AddRange(BitConverter.GetBytes(OPCODE));
            packet.AddRange(BitConverter.GetBytes(SEQ_NO));
            packet.AddRange(BitConverter.GetBytes(LENGTH));
            packet.AddRange(BitConverter.GetBytes(CRC));
            packet.AddRange(BODY);

            return packet.ToArray();
        }

        ///....... 이런 식으로 작성하면 프로토콜 프레임이 작성되고
        ///.......  바이트배열이 만들어 지면 전송객체에 전달해주면 된다.
        // ......  주의 사항으로 바이트오더에 주의
    }
}

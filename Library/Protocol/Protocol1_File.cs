using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocols
{
    public class Protocol1_File_Log
    {
        public int name_legth;
        public string name;
        public int file_size;

        public Protocol1_File_Log(int Name_Length, string Name_, int File_Size)
        {
            name_legth = Name_Length;
            name = Name_;
            file_size = File_Size;
        }
    }

    public class Protocol1_File
    {
        public static byte[] FileToBinary(string root, string name)
        {
            string filename = root + name;
            string fullPath = Path.GetFullPath(filename);
            var file = new FileInfo(fullPath);
            byte[] binary = new byte[file.Length]; // 바이너리 버퍼

            if (file.Exists)
            {
                var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                stream.Read(binary, 0, binary.Length);
            }

            return binary;
        }
        public static byte[] FileToBinary(string fullPath)
        {
            var file = new FileInfo(fullPath);
            byte[] binary = new byte[file.Length]; // 바이너리 버퍼

            if (file.Exists)
            {
                var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                stream.Read(binary, 0, binary.Length);
            }

            return binary;
        }

        public static Byte[] TransmitFileRequest(int name_legth, string name, int file_size)
        {
            List<byte> request = new List<byte>();
            request.AddRange(BitConverter.GetBytes(name_legth));
            request.AddRange(Encoding.UTF8.GetBytes(name));
            request.AddRange(BitConverter.GetBytes(file_size));
            return request.ToArray();
        }
        public static int TransmitFileResponse(string filename, int size)
        {
            /// 파일 이름 길이 20글자 제한
            if (filename.Length > 40)
            {
                //return Encoding.UTF8.GetBytes("101_filename is too long");
                //return BitConverter.GetBytes(101);
                return 101;
            }

            else if (size > int.MaxValue)
            {
                //return Encoding.UTF8.GetBytes("102_file is too big");
                //return BitConverter.GetBytes(102);
                return 102;
            }

            else
            {
                //return Encoding.UTF8.GetBytes("100_OK");
                //return BitConverter.GetBytes(100);
                return 100;
            }
        }

        public static List<Byte[]> TransmitFile(byte[] binary)
        {
            List<Byte[]> packets = new List<Byte[]>();
            int headerSize = 16;
            int bodySize = 4096 - headerSize - 1; /// 16의 배수로 맞출경우, 빈 블록이(16byte) 생겨 한 칸 비움
            int totalPackets = (int)Math.Ceiling((double)binary.Length / bodySize);

            for (int seq = 0; seq < totalPackets; seq++)
            {
                /// 패킷의 시퀀스에 다른 읽어들이기 offset 조정
                int dataStartIndex = seq * bodySize;
                int dataLength = Math.Min(bodySize, binary.Length - dataStartIndex);

                /// 패킷의 실제 데이터 배열을 생성
                byte[] dataChunk = new byte[dataLength];
                Array.Copy(binary, dataStartIndex, dataChunk, 0, dataLength);
                
                packets.Add(dataChunk);
            }

            /// 최대 4079 크기의 바이트배열 묶음 반환
            return packets;
        }
    }
}

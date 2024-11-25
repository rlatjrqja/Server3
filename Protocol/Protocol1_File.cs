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

        public static Byte[] TransmitFileRequest(int name_legth, string name, int file_size)
        {
            List<byte> request = new List<byte>();
            request.AddRange(BitConverter.GetBytes(name_legth));
            request.AddRange(Encoding.UTF8.GetBytes(name));
            request.AddRange(BitConverter.GetBytes(file_size));
            return request.ToArray();
        }
        public static Byte[] TransmitFileResponse(string filename, int size)
        {
            /// 파일 이름 길이 20글자 제한
            if (filename.Length > 40)
            {
                //return Encoding.UTF8.GetBytes("101_filename is too long");
                return BitConverter.GetBytes(101);
                
            }

            else if (size > int.MaxValue)
            {
                //return Encoding.UTF8.GetBytes("102_file is too big");
                return BitConverter.GetBytes(102);
            }

            else
            {
                //return Encoding.UTF8.GetBytes("100_OK");
                return BitConverter.GetBytes(100);
            }
        }

        public static List<Byte[]> TransmitFile(byte[] binary)
        {
            List<Byte[]> packets = new List<Byte[]>();
            int headerSize = 16;
            int maxDataSize = 4096 - headerSize - 1; // 암호화 버퍼 1
            int totalPackets = (int)Math.Ceiling((double)binary.Length / maxDataSize);

            for (int seq = 0; seq < totalPackets; seq++)
            {
                int dataStartIndex = seq * maxDataSize;
                int dataLength = Math.Min(maxDataSize, binary.Length - dataStartIndex);

                // 실제 데이터 배열을 생성
                if (dataLength < 2000)
                    Console.Write("Debug");
                byte[] dataChunk = new byte[dataLength];
                Array.Copy(binary, dataStartIndex, dataChunk, 0, dataLength);
                
                packets.Add(dataChunk);
            }

            return packets;
        }
    }
}

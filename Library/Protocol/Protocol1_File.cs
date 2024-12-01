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
        public static int TransmitFileResponse(string filename, int size, string dir)
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

            else if(File.Exists(dir + filename))
            {
                return 103;
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

        public static List<string> LoadFileList(string rootDir)
        {
            try
            {
                // 1. 상대 경로를 절대 경로로 변환
                string absolutePath = Path.GetFullPath(rootDir);

                // 2. 디렉토리가 존재하는지 확인
                if (!Directory.Exists(absolutePath))
                {
                    Console.WriteLine("해당 디렉토리가 존재하지 않습니다.");
                    return null;
                }

                // 3. 디렉토리 내 파일 이름 불러오기
                List<string> fileNames = new List<string>(Directory.GetFiles(absolutePath));

                // 파일 이름이 없을 경우 처리
                if (fileNames.Count == 0)
                {
                    Console.WriteLine("디렉토리에 파일이 없습니다.");
                    return null;
                }

                // 4. 파일 이름 리스트 출력 (한 줄에 하나씩)
                for (int i = 0; i < fileNames.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(fileNames[i])}");
                }

                return fileNames;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
                return null;
            }
        }
    }
}

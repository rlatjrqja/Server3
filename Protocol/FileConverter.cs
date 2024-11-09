using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Protocols
{
    public class FileConverter
    {
        public byte[] FileToByte(FileInfo file)
        {
            // 파일이 존재하는지
            if (file.Exists)
            {
                // 바이너리 버퍼
                var binary = new byte[file.Length];
                var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);

                // 파일을 IO로 읽어온다.
                stream.Read(binary, 0, binary.Length);
                return binary;
            }
            else return null;
        }

        public FileInfo ByteToFile(byte[] data)
        {
            //FileInfo file = new FileInfo();
            return null;
        }
    }
}

using System.Security.Cryptography;

namespace Integrity
{
    public class MySHA256
    {
        public static byte[] CreateHash(byte[] data)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(data);
            return bytes;
        }

        /// 클라이언트에서 전송받은 해시1, 파일 전송 이후 서버측에서 만들어낸 해시2를 비교
        public static bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length)
            {
                Console.WriteLine($"두 해시의 길이가 다릅니다. " +
                    $"HASH1: {hash1.Length}byte, " +
                    $"HASH2: {hash2.Length}byte");
                return false;
            }

            bool result = hash1.SequenceEqual(hash2);
            if (result) { Console.WriteLine("무결성 검증 완료"); }
            else { Console.WriteLine("두 패킷의 해시가 다릅니다."); }
            return result;
        }
    }
}

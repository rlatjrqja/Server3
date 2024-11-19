using System.Net.Sockets;
using System.Reflection.Metadata;
using ServerSocket;

namespace Server_main
{
    internal class Loop
    {
        static void Main(string[] args)
        {
            /// 추후 ID, Password를 구현한다면 Dictionary를 반환하는 라이브러리를 만드는 것 고려하기
            /// Json으로 저장 & 불러오기 해서 유저 테이블 만들기

            /// IP 바인드
            RootServer root = new RootServer("0.0.0.0", 50001);

            /// 서버 Listen 상태로 전환
            root.StartServer();

            /// 시스템 메세지 큐
            Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine($"...Server is running...Users on server: {root.GetUserCount()}");
                    Thread.Sleep(5000);
                }
                Console.WriteLine("......Server END");
            });

            /// 유저 접속 관리 (동기)
            while (true)
            {
                /// 유저를 접속 받고 리스트에 추가
                ClientHandle client = root.RunServer();
                Console.WriteLine($"{DateTime.Now}_New Client Added. From~[{client.host.RemoteEndPoint}]");

            }
        }
    }
}

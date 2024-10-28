using ServerSocket;
using System.Net.Sockets;

namespace Server_main
{
    internal class Loop
    {
        static List<Socket> users = new List<Socket>();

        static void Main(string[] args)
        {
            RootServer root = new RootServer("0.0.0.0", 50000);
            root.StartServer();

            /// 사용자 연결 시작
            Task.Run(() => {
                while (true)
                {
                    Socket? client = root.AddUsers();
                    if(client != null) users.Add(client);
                }
            });

            /// 지속적으로 띄울 메세지
            Task.Run(() =>
            {
                while(true)
                {
                    Thread.Sleep(5000);
                    Console.WriteLine($"...Server is running... Users on server: {users.Count}");
                }
            });
        }
    }
}

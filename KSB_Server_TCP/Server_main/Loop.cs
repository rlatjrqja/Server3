using ServerSocket;
using System.Net.Sockets;

namespace Server_main
{
    internal class Loop
    {
        static void Main(string[] args)
        {
            RootServer root = new RootServer("0.0.0.0", 50000);
            root.StartServer();

            Task.Run(() =>
            {
                while (true)
                {
                    root.RunServer();
                }
            });

            
            /// 메인 스레드가 종료되지 않도록 하는 무한 루프
            while (true)
            {
                try
                {
                    /// 지속적으로 띄울 메세지
                    Console.WriteLine($"...Server is running...Users on server: {root.GetUserCount()}");
                    Thread.Sleep(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    break;
                }
            }
            Console.WriteLine("......Server END");
        }
    }
}

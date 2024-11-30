using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocols;
using Encryption;
using Integrity;
using Login;
using System;

namespace KSB_Client_TCP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ip = "192.168.45.232"; // 고정 IP
            //string ip = "172.18.27.199"; // 고정 IP
            int port = 50001;            // 고정 포트 번호
            string rootDir = @"..\..\..\..\..\KSB_Client_TCP\files\";
            //string name = @"Dummy.xlsx";

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            host.Connect(ipep);

            bool running = Fuctions.InitializeConnection(host);
            while (running)
            {
                Console.Clear();
                Console.WriteLine("==== 클라이언트 메뉴 ====");
                Console.WriteLine("1. 회원가입");
                Console.WriteLine("2. 로그인");
                Console.WriteLine("3. Plane Text 전송");
                Console.WriteLine("4. 파일 전송");
                Console.WriteLine("5. 연결 끊기");
                Console.Write("메뉴를 선택하세요: ");

                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        // 회원가입
                        Console.WriteLine("회원가입 요청");
                        Fuctions.CreateAccount(host);
                        break;
                    case "2":
                        // 로그인
                        Console.WriteLine("로그인 요청");
                        Fuctions.TryLogin(host);
                        break;
                    case "3":
                        // Plane Text 전송
                        Console.WriteLine("메세지 전송");
                        Fuctions.TextTransfer(host);
                        break;
                    case "4":
                        // 파일 전송
                        Console.WriteLine("파일 전송을 시작합니다...");
                        string name = Fuctions.SelectFile(rootDir);
                        Fuctions.FileTransferRequest(host, rootDir, name);
                        break;
                    case "5":
                        // 연결 끊기
                        Console.WriteLine("서버와 연결을 종료합니다...");
                        Fuctions.Disconnect(host);
                        running = false;
                        break;
                    default:
                        Console.WriteLine("잘못된 입력입니다. 다시 선택하세요.");
                        break;
                }

                if (running)
                {
                    Console.WriteLine("\n계속하려면 Enter 키를 누르세요...");
                    Console.ReadLine();
                }
            }
        }
    }
}

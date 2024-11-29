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
            string rootDir = @"..\..\..\..\..\KSB_Client_TCP\files";
            string name = @"\Dummy.xlsx";

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            host.Connect(ipep);

            byte[] connection = Header.AssemblePacket(0, Const.CONNECT_REQUEST, 0, 1, 0, new byte[1]);
            host.Send(connection);

            Header response_connect = WaitForServerResponse(host);
            if (CheckOPCODE(response_connect, Const.CONNECT_REQUEST, "서버 접속 성공", "서버 접속 실패"))
            {

            }

            bool running = true;

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
                        // 클라이언트 소켓 생성 및 연결
                        //Dictionary<string, byte[]> info = UserInfo.CreateID();
                        //byte[] login_data = Protocol3_Json.DictionaryToJson(info);
                        Fuctions.CreateAccount(host);
                        Header response_create = WaitForServerResponse(host);
                        if (CheckOPCODE(response_create, Const.CREATE_ACCOUNT, "회원가입 성공", "가입 실패"))
                        {

                        }
                        else
                        {
                            string reason = Encoding.UTF8.GetString(response_create.BODY);
                            Console.WriteLine(reason);
                        }
                        break;

                    case "2":
                        // Plane Text 전송 - 아직 미구현
                        Fuctions.TryLogin(host);
                        Header response_login = WaitForServerResponse(host);
                        if (CheckOPCODE(response_login, Const.LOGIN, "로그인 성공", "로그인 실패"))
                        {

                        }
                        else
                        {
                            string reason = Encoding.UTF8.GetString(response_login.BODY);
                            Console.WriteLine(reason);
                        }
                        break;

                    case "3":
                        // Plane Text 전송 - 아직 미구현
                        Console.WriteLine("Plane Text 전송 기능은 아직 구현되지 않았습니다.");
                        break;

                    case "4":
                        // 파일 전송
                        Console.WriteLine("파일 전송을 시작합니다...");

                        Fuctions.FileTransferRequest(host, rootDir, name);
                        Header response_file = WaitForServerResponse(host);
                        if (CheckOPCODE(response_file, Const.FILE_REQUEST, "파일 전송 가능", "파일 전송 불가"))
                        {
                            Fuctions.FileTransfer(host, rootDir, name);
                        }
                        else
                        {
                            string reason = Encoding.UTF8.GetString(response_file.BODY);
                            Console.WriteLine(reason);
                        }

                        Header response_end = WaitForServerResponse(host);
                        if (CheckOPCODE(response_end, Const.SENDLAST, "마지막 패킷 수신 알림", "수신 중 이상 발생"))
                        {
                            // 해시 전송
                            Fuctions.FileCheckRequest(host, rootDir, name);
                        }
                        else
                        {
                            string reason = Encoding.UTF8.GetString(response_end.BODY);
                            Console.WriteLine(reason);
                        }
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

        /// <summary>
        /// 서버에 요청 보낸 뒤 응답을 대기 하는 용도
        /// </summary>
        public static Header WaitForServerResponse(Socket host)
        {
            byte[] buffer = new byte[4096];
            int bytesReceived = host.Receive(buffer);
            if (bytesReceived > 0)
            {
                Header header = new Header();
                header.DisassemblePacket(buffer);
                return header;
            }
            return null;
        }

        public static bool CheckOPCODE(Header hd, int opcode, string correctMSG, string failMSG)
        {
            if (hd.OPCODE == opcode)
            {
                Console.WriteLine(correctMSG);
                return true;
            }
            else
            {
                Console.WriteLine(failMSG);
                return false;
            }
        }
    }
}

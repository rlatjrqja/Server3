using Protocols;
using System.Net.Sockets;

namespace Server_main
{
    public class ClientHandle
    {
        public Socket host;

        public ClientHandle(Socket client)
        {
            host = client;

            Task.Run(() =>
            {
                while (true)
                {
                    byte[] packet = StartListening();
                    Header header = new Header();
                    header.MakePacket(packet);

                    switch(header.OPCODE)
                    {
                        case Const.CONNECT_REQUEST:
                            ConnectionRequestRecv();
                            break;
                        case Const.FILE_REQUEST:
                            SendFileRequestRecv();

                            break;
                        case Const.SENDING:
                            break;
                    }
                }
            });
        }

        public byte[] StartListening()
        {
            byte[] buffer = new byte[4096];
            int length = host.Receive(buffer);
            byte[] lawData = new byte[length];
            Array.Copy(buffer, lawData, length);

            return lawData;
        }

        void ConnectionRequestRecv()
        {
            foreach(var handle in RootServer.instance.users)
            {
                if(handle == this) return;
            }
            
            RootServer.instance.users.Add(this);
        }

        void SendFileRequestRecv()
        {

        }
    }
}

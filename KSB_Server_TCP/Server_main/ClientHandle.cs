using Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

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
            RootServer.instance.users.Add(this);
        }
    }
}

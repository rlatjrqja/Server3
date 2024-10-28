using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocket
{
    internal class ClientHandle
    {
        Socket host;

        public ClientHandle(Socket client) 
        {
            host = client;
        }
    }
}

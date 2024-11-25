using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Protocols
{
    public class Protocol2_Plain
    {
        public static string? GetOneMessage(ref string data)
        {
            int startIndex = data.IndexOf('\"');
            int endIndex = data.IndexOf('\"') + 1;
            if (startIndex < 0 || endIndex <= 0) return null;

            string message = data.Substring(startIndex, endIndex);

            data = data.Substring(endIndex);
            return message;
        }

    }
}

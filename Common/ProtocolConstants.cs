using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ProtocolConstants
    {
        public const int HEADER_SIZE = 3;
        public const int CMD_SIZE = 2;
        public const int LENGTH_SIZE = 4;

        public static string Request = "REQ";
        public static string Response = "RES";
    }
}


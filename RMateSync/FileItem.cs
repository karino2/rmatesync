using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMateSync
{
    public class FileItem
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string RealPath { get; set;  }
        public DateTime LastWrite { get; set; }
    }

    public class ServerItem
    {
        public int Id { get; set; }
        public string Path { get; set; }
    }
}

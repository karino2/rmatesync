using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMateSync
{

    interface ILineReader
    {
        string ReadLine();
    }

    class UnbufferedStreamReader : ILineReader
    {
        Stream _stream;
        public UnbufferedStreamReader(Stream basestream)
        {
            _stream = basestream;
        }

        public string ReadLine()
        {
            List<byte> bytes = new List<byte>();
            int current;
            while ((current = _stream.ReadByte()) != -1 && current != (int)'\n')
            {
                byte b = (byte)current;
                bytes.Add(b);
            }
            return Encoding.ASCII.GetString(bytes.ToArray());
        }


    }

}

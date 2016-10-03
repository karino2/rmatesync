using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMateSync
{
    class ConnectionHandler
    {
        TcpClient _client;
        ILineReader _reader;
        StreamWriter _writer;
        NetworkStream _stream;
        Store _store;

        public ConnectionHandler(TcpClient client, NetworkStream ns, StreamWriter sw, Store store)
        {
            _client = client;
            _reader = new UnbufferedStreamReader(ns);
            _stream = ns;
            _writer = sw;
            _store = store;
        }

        public void DoProcesss()
        {
            String msg = _reader.ReadLine();
            Debug.WriteLine(msg); // "open"

            var cmd = new OpenCommand(_reader, _stream, _store);
            while(!cmd.IsFinish)
            {
                cmd.ReadAndEvalOne();
                // Debug.WriteLine(cmd.DebToString());
            }
            Debug.WriteLine("done");

        }

    }
}

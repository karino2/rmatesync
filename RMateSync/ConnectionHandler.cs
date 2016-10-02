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

    class OpenCommand
    {
        enum State
        {
            PARSE_PARAM,
            PARSE_DATA,
            FINISH
        }

        public String Path { set; get; }

        UnbufferedStreamReader _reader;
        NetworkStream _stream;
        MemoryStream _data = new MemoryStream();
        State _state = State.PARSE_PARAM;
        Dictionary<String, String> _options = new Dictionary<string, string>();

        public OpenCommand(UnbufferedStreamReader sr, NetworkStream ns)
        {
            _reader = sr;
            _stream = ns;
            DataLength = 0;
        }

        public bool IsFinish
        {
            get
            {
                return _state == State.FINISH;
            }
        }

        public String DebToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach(var pair in _options)
            {
                builder.Append("option[");
                builder.Append(pair.Key);
                builder.Append("] = ");
                builder.Append(pair.Value);
                builder.Append("\n");
            }
            builder.Append("datalen:");
            builder.Append(DataLength);
            builder.Append("\n");
            builder.Append(DebGetContents());
            return builder.ToString();
        }

        public String DebGetContents()
        {
            long orgpos = _data.Position;
            _data.Position= 0;
            var tmpReader = new StreamReader(_data);
            var res = tmpReader.ReadToEnd();
            _data.Position = orgpos;
            return res;
        }

        public int DataLength { set; get; }

        public void HandleParam(string line)
        {
            var seps = line.Split(':');
            var key = seps[0].Trim();
            var val = seps[1].Trim();

            if ("data" == key)
            {
                DataLength = int.Parse(val);
                _state = State.PARSE_DATA;
            } else
            {
                _options[key] = val;
            }
        }

        public void ReadData()
        {
            int sizeRead = 0;
            byte[] buf = new byte[1024];
            
            while(sizeRead < DataLength)
            {
                var count = _stream.Read(buf, 0, Math.Min(buf.Length, DataLength - sizeRead));
                _data.Write(buf, 0, count);
                sizeRead += count;
            }

            _state = State.FINISH;
        }

        public void ReadAndEvalOne()
        {
            switch(_state)
            {
                case State.PARSE_PARAM:
                    HandleParam(_reader.ReadLine());                    
                    return;
                case State.PARSE_DATA:
                    ReadData();
                    return;
                case State.FINISH:
                    return;
            }
        }


    }

    class UnbufferedStreamReader
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

    class ConnectionHandler
    {
        TcpClient _client;
        UnbufferedStreamReader _reader;
        StreamWriter _writer;
        NetworkStream _stream;
        public ConnectionHandler(TcpClient client, NetworkStream ns, StreamReader sr, StreamWriter sw)
        {
            _client = client;
            _reader = new UnbufferedStreamReader(ns);
            _stream = ns;
            _writer = sw;
        }

        public void DoProcesss()
        {
            String msg = _reader.ReadLine();
            Debug.WriteLine(msg); // "open"

            var cmd = new OpenCommand(_reader, _stream);
            while(!cmd.IsFinish)
            {
                cmd.ReadAndEvalOne();
                Debug.WriteLine(cmd.DebToString());
            }
            Debug.WriteLine("done");

        }

    }
}

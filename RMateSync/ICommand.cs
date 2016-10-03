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
    interface ICommand
    {
        void ReadAndEvalOne();
        bool IsFinish { get;  }
    }

    class FileOpener
    {
        public void Open(FileInfo path)
        {
            var settings = Settings.Create();



            var proc = new Process();
            proc.StartInfo.FileName = settings.ReadEditorPath();
            proc.StartInfo.Arguments = BuildArguments(settings.ReadEditorArgs(), path.FullName);
            proc.Start();
        }

        string BuildArguments(string args, string pathname)
        {
            if (String.IsNullOrEmpty(args))
                return "\"" + pathname + "\"";
            return args + " \"" + pathname + "\"";

        }
    }

    class FileSaver
    {
        DirectoryInfo _baseDir;
        Store _store;
        public FileSaver(DirectoryInfo basedir, Store store)
        {
            _baseDir = basedir;
            _store = store;
        }

        String ServerName(IDictionary<string, string> options)
        {
            String dispName = options["display-name"];
            var seps = dispName.Split(':');
            if (seps.Length > 1)
                return seps[0];
            return dispName;
        }

        void EnsureDir(DirectoryInfo dir)
        {
            if (!dir.Exists)
                dir.Create();
        }


        public Tuple<FileInfo, string> Save(OpenCommand data)
        {
            DirectoryInfo serverDir = new DirectoryInfo(Path.Combine(_baseDir.FullName, ServerName(data.Options)));
            EnsureDir(serverDir);
            _store.SaveServer(serverDir);

            var realpath = data.Options["real-path"];
            // cut first / .
            FileInfo savepath = new FileInfo(Path.Combine(serverDir.FullName, realpath.Substring(1)));

            EnsureDir(savepath.Directory);
            if (savepath.Exists)
                savepath.Delete();

            using (var writer = savepath.OpenWrite())
            {
                data.Contents.WriteTo(writer);
            }

            return new Tuple<FileInfo, string>(savepath, realpath);
        }


        public static FileSaver Create(Store store)
        {
            return new RMateSync.FileSaver(new DirectoryInfo(Environment.CurrentDirectory), store);
        }
    }

    class OpenCommand : ICommand
    {
        enum State
        {
            PARSE_PARAM,
            PARSE_DATA,
            SKIP_DOT,
            SAVE_AND_OPEN,
            FINISH
        }

        public String Path { set; get; }

        ILineReader _reader;
        NetworkStream _stream;
        MemoryStream _data = new MemoryStream();
        State _state = State.PARSE_PARAM;
        Dictionary<String, String> _options = new Dictionary<string, string>();
        Store _store;

        public IDictionary<String, String> Options {  get { return _options;  } }
        public MemoryStream Contents {  get { return _data;  } }

        public OpenCommand(ILineReader sr, NetworkStream ns, Store store)
        {
            _reader = sr;
            _stream = ns;
            _store = store;
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
            foreach (var pair in _options)
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
            _data.Position = 0;
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
            }
            else
            {
                _options[key] = val;
            }
        }

        public void ReadData()
        {
            int sizeRead = 0;
            byte[] buf = new byte[1024];

            while (sizeRead < DataLength)
            {
                var count = _stream.Read(buf, 0, Math.Min(buf.Length, DataLength - sizeRead));
                _data.Write(buf, 0, count);
                sizeRead += count;
            }

            _state = State.SKIP_DOT;
        }

        public void ReadAndEvalOne()
        {
            switch (_state)
            {
                case State.PARSE_PARAM:
                    HandleParam(_reader.ReadLine());
                    return;
                case State.PARSE_DATA:
                    ReadData();
                    return;
                case State.SKIP_DOT:
                    _reader.ReadLine(); // discard last .\n
                    _state = State.SAVE_AND_OPEN;
                    return;
                case State.SAVE_AND_OPEN:
                    SaveAndOpen();
                    return;
                case State.FINISH:
                    return;
            }
        }

        private void SaveAndOpen()
        {
            var saver = FileSaver.Create(_store);
            var pathtupple = saver.Save(this);

            var fitem = new FileItem { Path = pathtupple.Item1.FullName, RealPath = pathtupple.Item2, LastWrite = DateTime.Now };
            _store.SaveFileItem(fitem);


            var opener = new FileOpener();
            opener.Open(pathtupple.Item1);

            _state = State.FINISH;
        }
    }


}

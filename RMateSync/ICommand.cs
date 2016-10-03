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
        public FileSaver(DirectoryInfo basedir)
        {
            _baseDir = basedir;
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


        public FileInfo Save(OpenCommand data)
        {
            DirectoryInfo serverDir = new DirectoryInfo(Path.Combine(_baseDir.FullName, ServerName(data.Options)));
            EnsureDir(serverDir);

            // cut first / .
            FileInfo savepath = new FileInfo(Path.Combine(serverDir.FullName, data.Options["real-path"].Substring(1)));

            EnsureDir(savepath.Directory);
            using (var writer = savepath.OpenWrite())
            {
                data.Contents.WriteTo(writer);
            }

            return savepath;
        }


        public static FileSaver Create()
        {
            return new RMateSync.FileSaver(new DirectoryInfo(Environment.CurrentDirectory));
        }
    }

    class OpenCommand : ICommand
    {
        enum State
        {
            PARSE_PARAM,
            PARSE_DATA,
            SAVE_AND_OPEN,
            FINISH
        }

        public String Path { set; get; }

        ILineReader _reader;
        NetworkStream _stream;
        MemoryStream _data = new MemoryStream();
        State _state = State.PARSE_PARAM;
        Dictionary<String, String> _options = new Dictionary<string, string>();

        public IDictionary<String, String> Options {  get { return _options;  } }
        public MemoryStream Contents {  get { return _data;  } }

        public OpenCommand(ILineReader sr, NetworkStream ns)
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

            _state = State.SAVE_AND_OPEN;
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
                case State.SAVE_AND_OPEN:
                    SaveAndOpen();
                    return;
                case State.FINISH:
                    return;
            }
        }

        private void SaveAndOpen()
        {
            var saver = FileSaver.Create();
            var path = saver.Save(this);

            var opener = new FileOpener();
            opener.Open(path);

            _state = State.FINISH;
        }
    }


}

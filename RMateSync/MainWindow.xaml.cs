using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RMateSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        const int PORT_NUM = 52698;


        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            dp("do nothing now");
        }

        

        Dictionary<string, FileSystemWatcher> _serverWatcher = new Dictionary<string, FileSystemWatcher>();

        void CheckNewServer(Store store)
        {
            var servers = store.FindAllServerItem();
            foreach(var server in servers)
            {
                FileSystemWatcher watcher;
                if(!_serverWatcher.TryGetValue(server.Path, out watcher))
                {
                    watcher = new FileSystemWatcher();
                    watcher.Path = server.Path;
                    watcher.IncludeSubdirectories = true;
                    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                    watcher.Changed += (o, e) => OnFileUpdate(e, server);
                    watcher.Renamed += (o, e) => OnFileRenamed(e, server);
                    _serverWatcher[server.Path] = watcher;
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        private void OnFileRenamed(RenamedEventArgs e, ServerItem server)
        {
            dp("rename from:  " + e.OldFullPath + ", to: " + e.FullPath);
            SendSaveRequest(e.FullPath, server);
        }

        private void OnFileUpdate(FileSystemEventArgs e, ServerItem server)
        {
            SendSaveRequest(e.FullPath, server);
        }


        // LiteDB is not thread safe, so I must create GUI thread one and accept thread one.
        void EnsureStore()
        {
            if (_store == null)
            {
                var db = CreateDBInstance();
                _store = new Store(db);
            }
        }

        private void SendSaveRequest(string filePath, ServerItem server)
        {
            EnsureStore();

            var fitem = _store.FindFileItem(filePath);
            if (fitem == null)
                return;

            dp("token:" + fitem.RealPath);
            // Debug.WriteLine(token);
            // retrieve token. send save command.

            FileInfo file = new FileInfo(fitem.Path);

            // should not close connection, so do not use using()
            var stream = _commandConnection.GetStream();
            var sw = new StreamWriter(stream);

            sw.WriteLine("save");
            sw.WriteLine("token: " + fitem.RealPath);
            sw.WriteLine("data : " + file.Length);
            sw.Flush();


            using (var fstream = file.OpenRead())
            {
                fstream.CopyTo(stream);
            }
            sw.WriteLine("");
            sw.Flush();
        }

        Store _store;

        LiteDatabase CreateDBInstance()
        {
            return new LiteDatabase("Files.db");
        }

        void serverLoop()
        {

            dp("server loop start");
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, PORT_NUM);
                using (var db = CreateDBInstance())
                {
                    var store = new Store(db);

                    listener.Start();
                    while (true)
                    {
                        var client = listener.AcceptTcpClient();
                        DebugDump(client, store);
                        CheckNewServer(store);

                        Thread.Sleep(100);
                    }
                }
            }catch(Exception e)
            {
                dp("server loop exit. Please relaunch manually.");
                dp("reason: " + e.Message);
            }
        }

        TcpClient _commandConnection = null;

        void ReclaimConnection(TcpClient client)
        {
            if(_commandConnection == null || !_commandConnection.Connected)
            {
                _commandConnection = client;
                return;
            }
            client.Close();
        }

        void DebugDump(TcpClient client, Store store)
        {
            // dp("dd1");

            // should not close connection, so do not use using()
            /*
            using (NetworkStream stream = client.GetStream())
            using(StreamWriter sw = new StreamWriter(stream))
            */
            var stream = client.GetStream();
            var sw = new StreamWriter(stream);
            {
                sw.WriteLine("MyServer Name");
                sw.Flush();
                ConnectionHandler handler = new ConnectionHandler(client, stream, sw, store);
                handler.DoProcesss();

                /*
                string line = sr.ReadLine();
                dp(line);
                */
            }
            ReclaimConnection(client);
            // dp("dd2");
        }


        private void dp(String msg)
        {
            debugWriteLine(msg);
        }

        private void debugWriteLine(string msg)
        {
            debugWrite(msg + "\n");
        }

        private void debugWrite(string msg)
        {
            Dispatcher.Invoke(() => debugBox.Text = msg + debugBox.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => serverLoop());
        }
    }
}

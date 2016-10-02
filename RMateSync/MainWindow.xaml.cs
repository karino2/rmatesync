using System;
using System.Collections.Generic;
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


        void test1()
        {
            dp("test1 start");
            var listener = new TcpListener(IPAddress.Loopback, PORT_NUM);

            listener.Start();
            while (true)
            {
                var client = listener.AcceptTcpClient();
                DebugDump(client);

                Thread.Sleep(100);
            }

        }

        void DebugDump(TcpClient client)
        {
            // dp("dd1");
            using (NetworkStream stream = client.GetStream())
            using(StreamWriter sw = new StreamWriter(stream))
            using (StreamReader sr = new StreamReader(stream))
            {
                sw.WriteLine("MyServer Name");
                sw.Flush();
                ConnectionHandler handler = new ConnectionHandler(client, stream, sr, sw);
                handler.DoProcesss();

                /*
                string line = sr.ReadLine();
                dp(line);
                */
            }
            client.Close();
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
            Dispatcher.Invoke(() => debugBox.Text += msg);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => test1());
        }
    }
}

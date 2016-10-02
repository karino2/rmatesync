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

        public ManualResetEvent tcpClientConnected =
            new ManualResetEvent(false);


        void test1()
        {
            dp("test1 start");
            // Parse("127.0.0.1")
            var listener = new TcpListener(IPAddress.Loopback, PORT_NUM);
            // var listener = new TcpListener(IPAddress.Any, PORT_NUM);

            listener.Start();
            while (true)
            {
                /*
                listener.BeginAcceptTcpClient(
                    new AsyncCallback(TcpClientCallback),
                    listener);
                    */
                var client = listener.AcceptTcpClient();
                // var client = listener.AcceptSocket();
                DebugDump(client);

                Thread.Sleep(100);
                // tcpClientConnected.WaitOne();
            }

        }

        private void TcpClientCallback(IAsyncResult ar)
        {
            var listener = (TcpListener)ar.AsyncState;

            TcpClient client = listener.EndAcceptTcpClient(ar);

            client.ReceiveBufferSize = 1;

            DebugDump(client);

            tcpClientConnected.Set();
        }

        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 256;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }

        void DebugDumpS(Socket client)
        {
            dp("dds1");
            StateObject state = new StateObject();
            state.workSocket = client;

            // Begin receiving the data from the remote device.
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            dp("dds2");
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;
            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);
            dp("dds3");
            if (bytesRead > 0)
            {
                String responseData = System.Text.Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                debugWriteLine(responseData);


                /*
                //  Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
                    */
            }
            else
            {
                dp("bytesread not positive");
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
                string line = sr.ReadLine();
                dp(line);
            }
            // dp("dd2");
        }

        void DebugDump2(TcpClient client)
        {
            /*
            using (NetworkStream stream = client.GetStream())
            {
                dp("deb2");
                */


                Byte[] data = new Byte[10];
                // while (stream.CanRead) {
                    /*
                        if(!stream.DataAvailable)
                        {
                            Thread.Sleep(100);
                        }else { 
                        {
                        */
                    /*
                    client.Available
                    dp(client.Available.ToString());
                    return;
                    */
                    // Int32 bytes = stream.Read(data, 0, Math.Min(data.Length, client.Available));
                    /*
                    stream.ReadTimeout = 500;
                    try { 
                        Int32 bytes = stream.Read(data, 0, 1);
                        String responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                        debugWriteLine(responseData);
                    }catch(IOException e)
                        {
                            dp("timeout");
                        }
                        */
                        // return;

            /*        
                }
                dp("deb3");
            }
            */
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

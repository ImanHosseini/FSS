using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace ConsoleApp1
{

    class Server
    {
        static string DEFAULT_IPADD = "127.0.0.1";
        static short DEFAULT_PORT = 8001;
        public Socket sock;
        Receiver rcv;
        Sender snd;
        Capturer cap;
        public byte[] buff;
        public Object thislock = new object();
        Dictionary<string, string> opts = new Dictionary<string, string>();

        public Server()
        {
            IPAddress ipadd = IPAddress.Parse(DEFAULT_IPADD);
            TcpListener tcpli = new TcpListener(ipadd, DEFAULT_PORT);
            tcpli.Start();
            sock = tcpli.AcceptSocket();
            Console.WriteLine("connected");
            rcv = new Receiver(sock);
            cap = new Capturer(this);
            snd = new Sender(this);
        }

        public Server(Dictionary<string, string> opts)
        {
            this.opts = opts;
            IPAddress ipadd = IPAddress.Parse(DEFAULT_IPADD);
            TcpListener tcpli = new TcpListener(ipadd, DEFAULT_PORT);
            tcpli.Start();
            sock = tcpli.AcceptSocket();
            Console.WriteLine("connected");
            rcv = new Receiver(sock);
            cap = new Capturer(this);
            snd = new Sender(this);
        }

    }
    class Program
    {
        

        static void Main(string[] args)
        {
            Server srv = new Server();
        }
    }


    class Receiver
    {
        Socket s;
        public Receiver(Socket s)
        {
            this.s = s;
            Thread newThread = new Thread(new ThreadStart(Run));
            newThread.Start();
        }

        public void Run()
        {

            Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);
            while (true)
            {
                byte[] data = new byte[1024];
                int receivedDataLength = s.Receive(data);
                string stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                Console.WriteLine(stringData);
            }
        }

    }


    class Sender
    {
        Server srvr;
        public Sender(Server srvr)
        {
            this.srvr = srvr;
            Thread newThread = new Thread(new ThreadStart(Run));
            newThread.Start();
        }

        public void Run()
        {
            while (true)
            {
                lock (srvr.thislock)
                {
                    if (srvr.buff != null)
                    {
                        srvr.sock.Send(srvr.buff);
                        Console.Write("$"+srvr.buff.Length+"$");
                    }
                }
                //Thread.Sleep(1200);
                
            }
            
         
        }

    }

    class Capturer
    {
        Server srvr;
        public Capturer(Server srvr)
        {
            this.srvr = srvr;
            Thread newThread = new Thread(new ThreadStart(Run));
            newThread.Start();
        }

        public void Run()
        {
            while (true)
            {
                //Create a new bitmap.
                var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                               Screen.PrimaryScreen.Bounds.Height,
                                               PixelFormat.Format32bppArgb);

                // Create a graphics object from the bitmap.
                var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

                // Take the screenshot from the upper left corner to the right bottom corner.
                gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                            Screen.PrimaryScreen.Bounds.Y,
                                            0,
                                            0,
                                            Screen.PrimaryScreen.Bounds.Size,
                                            CopyPixelOperation.SourceCopy);

                using (MemoryStream ms = new MemoryStream())
                {
                    bmpScreenshot.Save(ms, ImageFormat.Png);


                    byte[] imageBuffer = ms.GetBuffer();
                    byte[] header = BitConverter.GetBytes(imageBuffer.Length);
                    lock (srvr.thislock)
                    {
                        srvr.buff = new byte[4 + imageBuffer.Length];
                        System.Buffer.BlockCopy(header, 0, srvr.buff, 0, 4);
                        System.Buffer.BlockCopy(imageBuffer, 0, srvr.buff, 4, imageBuffer.Length);
                    }

                    Thread.Sleep(2000);
                    // Get the elapsed time as a TimeSpan value.

                    // Console.Write(imageBuffer.Length + "*");
                    //Console.WriteLine(bmpScreenshot.Height + "AND" + bmpScreenshot.Width);
                }
            }

        }

    }
}

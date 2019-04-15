using System;

using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using System.Threading;
using System.Net;
using System.Net.Sockets;
using Nito.Collections;


namespace Proto1
{
    class Proto1
    {
        public VDeq deque;
        Socket sock;
        public int num = 0; 

        internal class VDeq
        {
            internal VNode front;
            internal VNode back;
            internal int size;
            public VDeq()
            {
                this.size = 0;
                this.front = null;
                this.back = null;
            }
            public void dump()
            {
                if (size == 0)
                {
                    Console.WriteLine("empty");
                    return;
                }

                var next = back;
                while (next != null)
                {
                    Console.Write(" *" + next.dumpStr() + " --> ");
                    next = next.next;
                }
                Console.Write("\n");



            }

        }

        static void AddToFront(byte[] data, VDeq vd)
        {
            if (vd.front == null)
            {
                VNode vn = new VNode(data, null, null);
                vd.front = vn;
                vd.back = vn;
            }
            else
            {
                VNode vn = new VNode(data, null, vd.front);
                vd.front.next = vn;
                vd.front = vn;
                vd.size++;
            }

        }
        static VNode PopFromBack(VDeq vd)
        {
            if (vd.size > 0)
            {
                VNode tmp = vd.back;
                vd.back.next.prev = null;
                vd.back = vd.back.next;
                vd.size--;
                return tmp;
            }
            return null;
        }
        internal class VNode
        {

            public VNode(byte[] data, VNode next, VNode prev)
            {
                this.data = data;
                this.next = next;
                this.prev = prev;
            }
            public string dumpStr()
            {
                return BitConverter.ToInt32(data, 0).ToString();
            }
            public byte[] data;
            public VNode next;
            public VNode prev;
        }



        public static bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return false;
            }
        }

        public Proto1() {

            IPAddress ipadd = IPAddress.Parse(DEFAULT_IPADD);
            TcpListener tcpli = new TcpListener(ipadd, DEFAULT_PORT);
            tcpli.Start();
            sock = tcpli.AcceptSocket();
            Console.WriteLine("connected");
            deque = new VDeq();
            Receiver rcv = new Receiver(sock);
            Sender snd = new Sender(this);

            var screenStateLogger = new ScreenStateLogger();

            Object thisLock = new Object();
            int cnt = 0;
            num = 0;

            screenStateLogger.ScreenRefreshed += (sender, data) =>
            {
                if (deque.size > 30) return;
                Console.WriteLine("backlog: " + deque.size);
                AddToFront(data,deque);
                // Console.WriteLine(deque.size);
               


                //(new Thread(() =>
                //{

                //    cnt++;
                //    Console.WriteLine("in thread ");
                //    byte[] header = BitConverter.GetBytes(data.Length);
                //    byte[] buff = new byte[4 + data.Length];
                //    System.Buffer.BlockCopy(header, 0, buff, 0, 4);
                //    System.Buffer.BlockCopy(data, 0, buff, 4, data.Length);
                //    Console.WriteLine("pre send "+ cnt );
                //    try
                //    {
                //        sock.Send(buff);
                //        ByteArrayToFile("a" + num + ".bmp", data);
                //        num++;

                //    } catch (Exception e)
                //    {
                //        Console.WriteLine(e);
                //    }


                //        Console.WriteLine("send and cnt is " + cnt);

                //    cnt--;

                //})).Start();

            };
            screenStateLogger.Start();

        }


        static string DEFAULT_IPADD = "127.0.0.1";
        static short DEFAULT_PORT = 8001;

        static void Main(string[] args)
        {

            Proto1 pr = new Proto1();

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
            Proto1 pr;
            public Sender(Proto1 pr)
            {
                this.pr = pr;
                Thread newThread = new Thread(new ThreadStart(Run));
                newThread.Start();
            }

            public void Run()
            {


                while (true)
                {
                    if (pr.deque.size > 1)
                    {
                        // Stopwatch sw = new Stopwatch();

                        // sw.Start();
                        byte[] imageBuffer = PopFromBack(pr.deque).data;
                        byte[] dat = new byte[4 + imageBuffer.Length];
                        byte[] header = BitConverter.GetBytes(imageBuffer.Length);
                        System.Buffer.BlockCopy(header, 0, dat, 0, 4);
                        System.Buffer.BlockCopy(imageBuffer, 0, dat, 4, imageBuffer.Length);
                        // Console.WriteLine("Sending " + imageBuffer.Length);
                        
                        // sw.Stop();
                        // Console.WriteLine("POP AND PAD milisec: " + sw.ElapsedMilliseconds);
                        // sw = new Stopwatch();
                        // sw.Start();
                        pr.sock.Send(dat);
                        // ByteArrayToFile("a" + pr.num + ".bmp", imageBuffer);
                        pr.num++;
                        // sw.Stop();
                        // Console.WriteLine("SEND milisec: " + sw.ElapsedMilliseconds);

                    }
                }
            }

        }

        public class ScreenStateLogger
        {
            private byte[] _previousScreen;
            private bool _run, _init;

            public int Size { get; private set; }
            public ScreenStateLogger()
            {

            }

            public void Start()
            {
                _run = true;
                var factory = new Factory1();
                //Get first adapter
                var adapter = factory.GetAdapter1(0);
                //Get device from adapter
                var device = new SharpDX.Direct3D11.Device(adapter);
                //Get front buffer of the adapter
                var output = adapter.GetOutput(0);
                var output1 = output.QueryInterface<Output1>();

                // Width/Height of desktop to capture
                int width = output.Description.DesktopBounds.Right;
                int height = output.Description.DesktopBounds.Bottom;

                // Create Staging texture CPU-accessible
                var textureDesc = new Texture2DDescription
                {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = width,
                    Height = height,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = { Count = 1, Quality = 0 },
                    Usage = ResourceUsage.Staging
                };
                var screenTexture = new Texture2D(device, textureDesc);

                Task.Factory.StartNew(() =>
                {
                    // Duplicate the output
                    using (var duplicatedOutput = output1.DuplicateOutput(device))
                    {
                        while (_run)
                        {
                            try
                            {
                                SharpDX.DXGI.Resource screenResource;
                                OutputDuplicateFrameInformation duplicateFrameInformation;

                                // Try to get duplicated frame within given time is ms
                                duplicatedOutput.AcquireNextFrame(10, out duplicateFrameInformation, out screenResource);

                                // copy resource into memory that can be accessed by the CPU
                                using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                                    device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                                // Get the desktop capture texture
                                var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                                // Create Drawing.Bitmap
                                using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                                {
                                    var boundsRect = new Rectangle(0, 0, width, height);

                                    // Copy pixels from screen capture Texture to GDI bitmap
                                    var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                                    var sourcePtr = mapSource.DataPointer;
                                    var destPtr = mapDest.Scan0;
                                    for (int y = 0; y < height; y++)
                                    {
                                        // Copy a single line 
                                        Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                                        // Advance pointers
                                        sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                                        destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                                    }

                                    // Release source and dest locks
                                    bitmap.UnlockBits(mapDest);
                                    device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                                    using (var ms = new MemoryStream())
                                    {
                                        bitmap.Save(ms, ImageFormat.Bmp);
                                        ScreenRefreshed?.Invoke(this, ms.ToArray());
                                        _init = true;
                                    }
                                }
                                screenResource.Dispose();
                                duplicatedOutput.ReleaseFrame();
                            }
                            catch (SharpDXException e)
                            {
                                if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                                {
                                    Trace.TraceError(e.Message);
                                    Trace.TraceError(e.StackTrace);
                                }
                            }
                        }
                    }
                });
                while (!_init) ;
            }

            public void Stop()
            {
                _run = false;
            }

            public EventHandler<byte[]> ScreenRefreshed;
        }
    }

}



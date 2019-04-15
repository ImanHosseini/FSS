# FSS
Fast Screen Server: A tcp server, sending screen in real-time. <br>
The trick to making this *really* fast is not to use **ScreenCapture** or other similar methods, but instead using **SharpDX** which is DirectX API (sadly no longer maintained) to get the frame buffer right from display memory. (The speed up changes the throughput by an order of magnitude) The architecture is also designed with consideration for real-time purposes: based on producer-consumer pattern, there are two threads sharing a queue data structure, the capture thread captures screen and enqueues frames and the network thread simultaneously dequeues and sends away the frames.<br>
Code for a receiver client is also included.

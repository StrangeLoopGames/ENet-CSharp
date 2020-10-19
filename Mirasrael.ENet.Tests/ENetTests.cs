namespace Mirasrael.ENet.Tests
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using global::ENet;
    using NUnit.Framework;

    public class Tests
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void TestENetIntercept()
        {
            using (var targetHost = new Host())
            {
                var targetAddress = new Address { Port = 10201 };
                targetAddress.SetHost("127.0.0.1");
                targetHost.Create(targetAddress, 1);

                var originalString = "Hello World";
                var receivedString = string.Empty;
                targetHost.RawDataReceived += (IntPtr address, IntPtr dataPtr, int length, ref bool consumed) =>
                {
                    var data = new byte[length];
                    Marshal.Copy(dataPtr, data, 0, (int)length);
                    receivedString = Encoding.UTF8.GetString(data);
                };

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SendTo(Encoding.UTF8.GetBytes("Hello World"), new IPEndPoint(IPAddress.Loopback, targetAddress.Port));

                Assert.AreEqual(0, targetHost.Service(100, out _));
                Assert.AreEqual(originalString, receivedString);
            }
        }

        [Test]
        public void TestENetRawMessages()
        {
            using(var targetHost = new Host())
            using (var host = new Host())
            {
                var targetAddress = new Address { Port = 10200 };
                targetAddress.SetHost("127.0.0.1");
                targetHost.Create(targetAddress, 1);

                var originalString = "Hello World";
                var receivedString = string.Empty;

                var address = new Address { Port = 10000 };
                address.SetIP("127.0.0.1");
                host.Create(address, 10, 2, 100, 200);

                targetHost.RawDataReceived += (IntPtr receivedAddressPtr, IntPtr dataPtr, int length, ref bool consumed) =>
                {
                    unsafe
                    {
                        var data            = new byte[length];
                        var receivedAddress = (Address*)receivedAddressPtr;
                        Assert.AreEqual(receivedAddress->Port, 10000);
                        Assert.AreEqual(receivedAddress->GetIP(), "127.0.0.1");
                        Marshal.Copy(dataPtr, data, 0, (int)length);
                        receivedString = Encoding.UTF8.GetString(data);
                    }
                };

                host.SendRaw(targetAddress, Encoding.UTF8.GetBytes($"++{originalString}++"), 2, Encoding.UTF8.GetBytes($"++{originalString}++").Length - 4);
                Assert.AreEqual(0, targetHost.Service(100, out _));
                Assert.AreEqual(originalString, receivedString);
            }
        }

        [Test]
        public void TestENetHostAddress()
        {
            using (var host = new Host())
            {
                var address = new Address { Port = 0 };
                address.SetIP("127.0.0.1");
                host.Create(address, 1);

                Assert.AreNotEqual(0, host.SocketAddress.Port);
                Assert.AreEqual("127.0.0.1", host.SocketAddress.GetIP());
                host.Dispose();
            }
        }

        [Test]
        public void TestENetPerformance()
        {
            Library.Initialize();
            try
            {
                using(var client = new Host())
                using (var server = new Host())
                {
                    client.Create(Address.AnyV4, 1);
                    client.RawDataReceived += (IntPtr ptr, IntPtr data, int length, ref bool consumed) => { };

                    var address = new Address();
                    address.Port = 10000;
                    address.SetIP("127.0.0.1");


                    server.Create(address, 10);
                    server.RawDataReceived += (IntPtr ptr, IntPtr data, int length, ref bool consumed) => { };

                    var netEvent = default(Event);
                    var peer     = client.Connect(address);
                    while (peer.State != PeerState.Connected)
                    {
                        if (server.Service(0, out netEvent) > 0)
                            if (netEvent.Type == EventType.Receive)
                                netEvent.Packet.Dispose();
                        if (client.Service(100, out netEvent) > 0)
                            if (netEvent.Type == EventType.Receive)
                                netEvent.Packet.Dispose();
                    }

                    var numMessages = 10000;
                    for (var i = 0; i < numMessages; i++)
                    {
                        var packet = default(Packet);
                        packet.Create(Encoding.UTF8.GetBytes("Hello"));
                        peer.Send(0, ref packet);
                    }

                    var packetsReceived = 0;
                    while (packetsReceived != numMessages)
                    {
                        if (client.Service(0, out netEvent) > 0)
                            if (netEvent.Type == EventType.Receive)
                                netEvent.Packet.Dispose();
                        if (server.Service(100, out netEvent) > 0)
                            if (netEvent.Type == EventType.Receive)
                            {
                                packetsReceived++;
                                netEvent.Packet.Dispose();
                            }
                    }
                }
            }
            finally
            {
                Library.Deinitialize();
            }
        }

        [Test]
        public void TestENetAddress()
        {
            Assert.AreEqual(default(Address).GetIP(), "::");

            void AssertIP(string ip)
            {
                var address = new Address();
                address.SetIP(ip);
                Assert.AreEqual(ip, address.GetIP());
                Assert.IsTrue(IPAddress.TryParse(ip, out _));
            }

            AssertIP("127.0.0.1");
            AssertIP("192.168.0.1");
            AssertIP("255.255.255.255");
            AssertIP("ff02::1");
            AssertIP("ff02::1:ff23:a050");
            AssertIP("0.0.0.0");
        }
    }
}
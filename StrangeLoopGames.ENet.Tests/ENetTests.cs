namespace StrangeLoopGames.ENet.Tests
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using NUnit.Framework;
    using global::ENet;
    using Event = global::ENet.Event;

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

                var  originalString = "Hello World";
                var  receivedString = string.Empty;
                using (SetInterceptCallback(targetHost, (ref Event @event, ref Address address, IntPtr dataPtr, int length) =>
                {
                    var data = new byte[length];
                    Marshal.Copy(dataPtr, data, 0, length);
                    receivedString = Encoding.UTF8.GetString(data);
                    return 0;
                }))
                {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.SendTo(Encoding.UTF8.GetBytes("Hello World"), new IPEndPoint(IPAddress.Loopback, targetAddress.Port));

                    Assert.AreEqual(0, targetHost.Service(100, out _));
                    Assert.AreEqual(originalString, receivedString);
                }
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

                using (SetInterceptCallback(targetHost, (ref Event @event, ref Address receivedAddress, IntPtr dataPtr, int length) =>
                {
                    var data = new byte[length];
                    Assert.AreEqual(receivedAddress.Port, 10000);
                    Assert.AreEqual(receivedAddress.GetIP(), "127.0.0.1");
                    Marshal.Copy(dataPtr, data, 0, length);
                    receivedString = Encoding.UTF8.GetString(data);
                    return 0;
                }))
                {
                    host.SendRaw(targetAddress, Encoding.UTF8.GetBytes($"++{originalString}++"), 2, Encoding.UTF8.GetBytes($"++{originalString}++").Length - 4);
                    Assert.AreEqual(0, targetHost.Service(100, out _));
                    Assert.AreEqual(originalString, receivedString);
                }
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

                    var address = new Address();
                    address.Port = 10000;
                    address.SetIP("127.0.0.1");

                    server.Create(address, 10);

                    using (SetInterceptCallback(client, (ref Event @event, ref Address receivedAddress, IntPtr data, int length) => 0))
                    using (SetInterceptCallback(server, (ref Event @event, ref Address receivedAddress, IntPtr data, int length) => 0))
                    {

                        Event netEvent;
                        var   peer = client.Connect(address);
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

        [Test]
        public void TestConnectionsExceed()
        {
            Library.Initialize();
            try
            {
                var server = new Host();
                server.Create(Address.AnyV4, 0);

                var client        = new Host();
                client.Create(Address.AnyV4, 1);
                var address = new Address { Port = server.SocketAddress.Port };
                address.SetIP("127.0.0.1");
                client.Connect(address);

                var maxAttempts       = 100;
                var connectionsExceed = false;
                while (!connectionsExceed && maxAttempts-- > 0)
                {
                    this.HandleNext(client, 0, e => connectionsExceed = e.Type == EventType.Notify && e.Data == (ulong)NotifyCode.ConnectionsExceed);
                    this.HandleNext(server, 1);
                }
                Assert.That(connectionsExceed, Is.True);
            }
            finally
            {
                Library.Deinitialize();
            }
        }

        [Test]
        public void TestErrorCode()
        {
            Library.Initialize();
            try
            {
                var serverOne = new Host();
                serverOne.Create(Address.AnyV4, 0);
                var serverTwo = new Host();
                Assert.That(() => serverTwo.Create(serverOne.SocketAddress, 0), Throws.Exception.TypeOf(typeof(ENetError)).With.Property(nameof(ENetError.Code)).EqualTo(ENetErrorCode.SocketBindFailed));
            }
            finally
            {
                Library.Deinitialize();
            }
        }

        private void HandleNext(Host host, int timeout, Action<Event> handler = null)
        {
            if (host.Service(timeout, out var netEvent) > 0)
            {
                handler?.Invoke(netEvent);
                if (netEvent.Type == EventType.Receive)
                    netEvent.Packet.Dispose();
            }
        }

        private struct CallbackRegistration : IDisposable
        {
            private InterceptCallback callback;

            public CallbackRegistration(InterceptCallback callback) { this.callback = callback; }
            public void Dispose() => this.callback = null;
        }

        private static CallbackRegistration SetInterceptCallback(Host host, InterceptCallback callback)
        {
            host.SetInterceptCallback(callback);
            return new CallbackRegistration(callback);
        }
    }
}
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
            var targetHost    = new Host();
            var targetAddress = new Address { Port = 10201 };
            targetAddress.SetHost("127.0.0.1");
            targetHost.Create(targetAddress, 1);

            var originalString = "Hello World";
            var receivedString = string.Empty;
            targetHost.RawDataReceived += (IntPtr address, IntPtr dataPtr, uint length, ref bool consumed) =>
            {
                var data = new byte[length];
                Marshal.Copy(dataPtr, data, 0, (int)length);
                receivedString = Encoding.UTF8.GetString(data);
            };

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SendTo(Encoding.UTF8.GetBytes("Hello World"), new IPEndPoint(IPAddress.Loopback, targetAddress.Port));

            Assert.AreEqual(0, targetHost.Service(100, out _));
            Assert.AreEqual(originalString, receivedString);
            targetHost.Dispose();
        }

        [Test]
        public void TestENetRawMessages()
        {
            var targetHost = new Host();
            var targetAddress = new Address { Port = 10200 };
            targetAddress.SetHost("127.0.0.1");
            targetHost.Create(targetAddress, 1);

            var originalString = "Hello World";
            var receivedString = string.Empty;

            var host = new Host();
            var address = new Address { Port = 10000 };
            address.SetIP("127.0.0.1");
            host.Create(address, 10, 2, 100, 200);

            targetHost.RawDataReceived += (IntPtr receivedAddressPtr, IntPtr dataPtr, uint length, ref bool consumed) =>
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
            targetHost.Dispose();
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
    }
}
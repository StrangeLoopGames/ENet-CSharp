namespace Mirasrael.ENet.Tests
{
    using global::ENet;
    using NUnit.Framework;

    public class Tests
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void TestENetHostStructure()
        {
            var host = new Host();
            var address = new Address { Port = 10000 };
            host.Create(address, 10, 2, 100, 200);
        }
    }
}
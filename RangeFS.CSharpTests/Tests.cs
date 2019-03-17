using System;
using Ranger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace RangeFS.CSharpTests
{

    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var a = (0).ToRange(10);
            var b = (10).ToRange(20);
            var c = a.Union(b);
            var d = (0).ToRange(20);
            Assert.AreEqual(c, d);
        }
    }
}

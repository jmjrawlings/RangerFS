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
            var a = (-10).ToRange(10);
            var n = Range.Iterate<int, int>(-1, a).ToList();
            Assert.IsTrue(n.Count == 1);
        }
    }
}

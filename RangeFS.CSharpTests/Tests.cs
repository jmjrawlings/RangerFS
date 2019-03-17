using System;
using Ranger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RangeFS.CSharpTests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var a = (-10).ToRange(10);
            var b = 0.ToRange().Buffer(10);
            var (c, d) = b.Map(i => i * 10).Bisect(50);
        }
    }
}

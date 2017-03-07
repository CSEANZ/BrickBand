using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageTests
{
    [TestClass]
    public class DeskewTest
    {
        [TestMethod]
        public async Task TestDeskew()
        {
            var f = System.Drawing.Image.FromFile("TestSkew.jpg");

            Assert.IsNotNull(f);
        }
    }
}

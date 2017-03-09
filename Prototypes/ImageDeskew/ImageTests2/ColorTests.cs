using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Services.ImageTools;

namespace ImageTests2
{
    [TestClass]
    public class ColorTests
    {
        [TestMethod]
        public void GetColorRow()
        {
            var c = new ColourFinder();

            var f = new FileInfo("sequence.jpg");
            var bm = new Image<Bgr, byte>(f.FullName);

            var colourCalibration = c.FindColors(bm.Bitmap);


            var f2 = new FileInfo("test_sequence.jpg");
            var bm2 = new Image<Bgr, byte>(f2.FullName);

            var colourRun = c.FindColors(bm2.Bitmap);


            var result = c.CompareColours(colourCalibration, colourRun);


        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Services.ImageTools
{
    public class ColourFinder
    {
        public void FindColors(Bitmap bm)
        {
            var origImage = new Image<Bgr, byte>(bm);

            var imgSmooth = origImage.SmoothMedian(41);

            Image<Hsv, Byte> hsvimg = imgSmooth.Convert<Hsv, Byte>();

            var byteList = new List<Tuple<int, Bgr>>();

            for (var c = 0; c < hsvimg.Cols; c += 2)
            {
                //var val = hsvimg.Mat.GetByteValue(hsvimg.Rows/2, c);

                var pixel = origImage[hsvimg.Rows / 2, c];

                byteList.Add(new Tuple<int, Bgr>(c, pixel));

                //byteList.Add(new Tuple<int, byte>(c, val));
                //if (val != 0)
                //{
                //    Debug.WriteLine(val);
                //    var two = val;

                //}
            }

            Mat lineImage = new Mat(origImage.Size, DepthType.Cv8U, 3);

            foreach (var p in byteList)
            {
                CvInvoke.Circle(lineImage, new Point(p.Item1, hsvimg.Rows / 2), 2, p.Item2.MCvScalar, 2);
            }

            lineImage.Bitmap.Save("Output_ColorTester.jpg", ImageFormat.Jpeg);

        }
    }
}

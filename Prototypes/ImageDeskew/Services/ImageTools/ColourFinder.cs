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

            var byteList = new List<Tuple<int, int, Bgr>>();


            int colums = 16;

            var pixPerCol = (hsvimg.Cols / 16);

            var colAverage = new Dictionary<int, Tuple<int, int, int, int, Bgr>>(); //count, totalb, totalg, totalr, pixelaverage

            for (var r = (hsvimg.Rows / 2) - 5; r < (hsvimg.Rows / 2) + 5; r += 1)
            {
                for (var c = 0; c < hsvimg.Cols; c += 1)
                {
                    var pixel = origImage[r, c];
                    byteList.Add(new Tuple<int, int, Bgr>(r, c, pixel));

                    

                    var thisCol = (int)(c / pixPerCol);

                    if (!colAverage.ContainsKey(thisCol))
                    {
                        colAverage.Add(thisCol, new Tuple<int, int, int, int, Bgr>(0, 0, 0, 0, default(Bgr)));
                    }

                    var tp = colAverage[thisCol];

                    var currentCount = tp.Item1 + 1;
                    var newTotalB = tp.Item2 + pixel.Blue;
                    var newTotalG = tp.Item3 + pixel.Green;
                    var newTotalR = tp.Item4 + pixel.Red;
                    
                    var averagePixel = new Bgr(newTotalB / currentCount, newTotalG / currentCount, newTotalR / currentCount);

                    var newTp = new Tuple<int, int, int, int, Bgr>(currentCount, (int)newTotalB, (int)newTotalG, (int)newTotalR, averagePixel);

            //var averageB = pixel.Blue * pixel.Blue;
            //var averageG = pixel.Green * pixel.Green;
            //var averageR = pixel.Red * pixel.Red;

            //var avgTotal = averageB + averageG + averageR;

            //var div2 = avgTotal / 2;

            //var averageColor = Convert.ToInt32(Math.Sqrt(div2));

       //     var newTp = new Tuple<int, int, int>(tp.Item1 + 1, tp.Item2 + averageColor, tp.Item2 + averageColor / tp.Item1 + 1);

                    colAverage[thisCol] = newTp;
                }
            }

            Mat lineImage = new Mat(origImage.Size, DepthType.Cv8U, 3);

            for (var c = 0; c < 16; c++)
            {
                var thisColData = colAverage[c];

                var pixelOffset = ((c + 1) * pixPerCol) / 2;

                CvInvoke.Circle(lineImage, new Point(pixelOffset, origImage.Height/2), 20, thisColData.Item5.MCvScalar, 2);
            }

            //foreach (var p in byteList)
            //{
            //    CvInvoke.Circle(lineImage, new Point(p.Item2, p.Item1), 2, p.Item3.MCvScalar, 2);
            //}

            lineImage.Bitmap.Save("Output_ColorTester.jpg", ImageFormat.Jpeg);

        }
    }
}


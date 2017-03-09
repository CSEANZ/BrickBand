using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using Services.Entity;

namespace Services.ImageTools
{
    public class ColourFinder
    {
        public List<ColourZone> CompareColours(List<ColourZone> calibration, List<ColourZone> compare)
        {
            var lActuals = new List<ColourZone>();
            foreach (var compCol in compare)
            {
                ColourZone currentColorZone = null;

                foreach (var calCol in calibration)
                {
                    if (compCol.Average >= calCol.Average - 5 && compCol.Average <= calCol.Average + 5)
                    {
                        currentColorZone = calCol;
                    }
                }

                lActuals.Add(currentColorZone ?? new ColourZone());
            }

            return lActuals;
        }


        public List<ColourZone> FindColors(Bitmap bm)
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

            var listOfColumns = new List<ColourZone>();

            foreach (var item in colAverage)
            {
                //thanks to https://medium.com/@kevinsimper/how-to-average-rgb-colors-together-6cd3ef1ff1e5#.3b9ktir0a for the colour average code!

                var cz = new ColourZone
                {
                    Column = item.Key,
                    B = Convert.ToByte(item.Value.Item5.Blue),
                    G = Convert.ToByte(item.Value.Item5.Green),
                    R = Convert.ToByte(item.Value.Item5.Red)
                };

                var averageB = cz.B * cz.B;
                var averageG = cz.G * cz.G;
                var averageR = cz.R * cz.R;

                var avgTotal = averageB + averageG + averageR;

                var div2 = avgTotal / 2;

                var averageColor = Convert.ToInt32(Math.Sqrt(div2));

                cz.Average = averageColor;

                listOfColumns.Add(cz);
                
            }

            var json = JsonConvert.SerializeObject(listOfColumns);

            File.WriteAllText("calibration.json", json);

            //foreach (var p in byteList)
            //{
            //    CvInvoke.Circle(lineImage, new Point(p.Item2, p.Item1), 2, p.Item3.MCvScalar, 2);
            //}

            lineImage.Bitmap.Save("Output_ColorTester.jpg", ImageFormat.Jpeg);

            return listOfColumns;

        }
    }
}


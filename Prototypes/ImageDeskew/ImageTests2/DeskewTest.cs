using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageTests
{
    [TestClass]
    public class DeskewTest
    {
       
        public async Task TestDeskew()
        {
            //var f = System.Drawing.Image.FromFile("TestSkew.jpg");

            //Assert.IsNotNull(f);

            var f = new FileInfo("TestSkew.jpg");

            Image<Gray, byte> image = new Image<Gray, byte>(f.FullName);
            double cannyThreshold = 10;
           double cannyThresholdLinking = 10;
            Gray circleAccumulatorThreshold = new Gray(500);
            Image<Gray, Byte> cannyEdges = image.Canny(cannyThreshold, cannyThresholdLinking);
            LineSegment2D[] lines = cannyEdges.HoughLinesBinary(
            4, //Distance resolution in pixel-related units
            Math.PI / 45.0, //Angle resolution measured in radians. ******
            100, //threshold
            2, //min Line width
            1 //gap between lines
            )[0]; //Get the lines from the first channel
            double[] angle = new double[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                double result = (double)(lines[i].P2.Y - lines[i].P1.Y) / (lines[i].P2.X - lines[i].P1.X);
                angle[i] = Math.Atan(result) * 57.2957795;
            }
            double avg = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                avg += angle[i];
            }
            avg = avg / lines.Length;
            Gray g = new Gray(255);
            Image<Gray, byte> imageRotate = image.Rotate(-avg, g);
            var bmp = imageRotate.Bitmap;
            bmp.Save("Output.jpg", ImageFormat.Jpeg);

        }

        [TestMethod]
        public async Task Test3()
        {
            double cannyThreshold = 80;
            double cannyThresholdLinking = 80;

            var file = new FileInfo("TestSkew2.jpg");
            
            var img = System.Drawing.Image.FromFile(file.FullName);
            var bitmap = new Bitmap(img);
            //var smooth = AdaptiveThresholdSmoothFilter(bitmap, 20, 20, 0.1);

            Image<Gray, byte> image = new Image<Gray, byte>(file.FullName);

            var imgSmooth = image.SmoothMedian(31);
            imgSmooth.Bitmap.Save("Smooth.jpg", ImageFormat.Jpeg);

            UMat cannyEdges = new UMat();
            CvInvoke.Canny(imgSmooth, cannyEdges, cannyThreshold, cannyThresholdLinking);

            var out2 = cannyEdges.Bitmap;

            out2.Save("output_Canny.jpg", ImageFormat.Jpeg);

            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, //Angle resolution measured in radians.
               20, //threshold
               30, //min Line width
               10); //gap between lines

            

            Mat lineImage = new Mat(image.Size, DepthType.Cv8U, 3);
            lineImage.SetTo(new MCvScalar(0));
            foreach (LineSegment2D line in lines)
                CvInvoke.Line(lineImage, line.P1, line.P2, new Bgr(Color.GreenYellow).MCvScalar, 2);

            var points = _findPoints(lines);

            CvInvoke.Line(lineImage, new Point(points.Item1, 0), new Point(points.Item1, image.Size.Height), new Bgr(Color.DodgerBlue).MCvScalar, 2);
            CvInvoke.Line(lineImage, new Point(points.Item2, 0), new Point(points.Item2, image.Size.Height), new Bgr(Color.DodgerBlue).MCvScalar, 2);
            CvInvoke.Line(lineImage, new Point(0, points.Item3), new Point(image.Size.Width, points.Item3), new Bgr(Color.DodgerBlue).MCvScalar, 2);
            CvInvoke.Line(lineImage, new Point(0, points.Item4), new Point(image.Size.Width, points.Item4), new Bgr(Color.DodgerBlue).MCvScalar, 2);
            

            var bmp = lineImage.Bitmap;

            bmp.Save("output_Lines.jpg", ImageFormat.Jpeg);
        }

        (int, int, int, int) _findPoints(LineSegment2D[] lines)
        {
            int leftX = int.MaxValue;
            int rightX = 0;
            int topY = int.MaxValue;
            int bottomY = 0;


            //var lowerX = line.P1.X < line.P2.X ? line.P1 : line.P2;

            //if (lowerX.X < leftX)
            //{
            //    leftX = lowerX.X;
            //    leftPoint = lowerX;
            //}


            foreach (var line in lines)
            {
                var lowerX = _findLower(line.P1.X, line.P2.X);

                if (lowerX < leftX)
                {
                    leftX = lowerX;
                }

                var upperX = _findUpper(line.P1.X, line.P2.X);

                if (upperX > rightX)
                {
                    rightX = upperX;
                }

                var upperY = _findUpper(line.P1.Y, line.P2.Y);
                //upper as in up the image, so lower value :/
                if (upperY < topY)
                {
                    topY = upperY;
                }

                var lowerY = _findLower(line.P1.Y, line.P2.Y);

                if (lowerY > bottomY)
                {
                    bottomY = lowerY;
                }
            }
            return (leftX, rightX, topY, bottomY);

        }

        int _findUpper(int a, int b)
        {
            return a > b ? a : b;
        }

        int _findLower(int a, int b)
        {
            return a < b ? a : b;
        }

        [TestMethod]
        public async Task Test2()
        {
            var f = new FileInfo("TestSkew.jpg");
            var result = Deskew(f.FullName);
            var bmp = result.Bitmap;
            bmp.Save("Output.jpg", ImageFormat.Jpeg);
        }

        public static Image<Gray, byte> Deskew(string file)
        {
            Image<Gray, byte> image = new Image<Gray, byte>(file);
            
            var bw = AdaptiveThresholdSmoothFilter(image.ToBitmap(), 20, 20, 0.03); //
            //CvInvoke.cvShowImage("", bw.Clone());
            LineSegment2D[] lines = bw.HoughLinesBinary(
            1, //Distance resolution in pixel-related units
            Math.PI / 45.0, //Angle resolution measured in radians. ******
            20, //threshold
            30, //min Line width
            10 //gap between lines
            )[0]; //Get the lines from the first channel
            double[] angle = new double[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                double result = (double)(lines[i].P2.Y - lines[i].P1.Y) / (lines[i].P2.X - lines[i].P1.X);
                angle[i] = Math.Atan(result) * 57.2957795;
            }
            double avg = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                avg += angle[i];
            }
            avg = avg / lines.Length;
            Gray g = new Gray(255);
            Image<Gray, byte> imageRotate = image.Rotate(-avg, g);
            return imageRotate;
        }
        public static Image<Gray, byte> AdaptiveThresholdSmoothFilter(Bitmap bmp, int width, int height, double c)
        {
            var gray = new Image<Gray, byte>(bmp);
            var smoothedGray = gray.SmoothBlur(width, height);

            var bw = smoothedGray - gray - c;
            
            return bw.ThresholdBinary(new Gray(0), new Gray(255));
        }
    }
}

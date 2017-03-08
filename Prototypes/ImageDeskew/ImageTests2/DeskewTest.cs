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
using Emgu.CV.Features2D;
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
            double cannyThreshold = 70;
            double cannyThresholdLinking = 70;

            var file = new FileInfo("TestSkew.jpg");

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

            foreach (var p in points)
            {
                CvInvoke.Circle(lineImage, new Point((int)p.X, (int)p.Y), 10, new Bgr(Color.Red).MCvScalar, 2);
            }

            var det = new GFTTDetector();
            MKeyPoint[] featPoints = det.Detect(cannyEdges, null);


            //foreach (var i in featPoints)
            //{
            //    CvInvoke.Circle(lineImage, new Point((int)i.Point.X, (int)i.Point.Y), 10, new Bgr(Color.Purple).MCvScalar, 2);
            //}

            //CvInvoke.Line(lineImage, new Point(points.Item1, 0), new Point(points.Item1, image.Size.Height), new Bgr(Color.DodgerBlue).MCvScalar, 2);
            //CvInvoke.Line(lineImage, new Point(points.Item2, 0), new Point(points.Item2, image.Size.Height), new Bgr(Color.DodgerBlue).MCvScalar, 2);
            //CvInvoke.Line(lineImage, new Point(0, points.Item3), new Point(image.Size.Width, points.Item3), new Bgr(Color.DodgerBlue).MCvScalar, 2);
            //CvInvoke.Line(lineImage, new Point(0, points.Item4), new Point(image.Size.Width, points.Item4), new Bgr(Color.DodgerBlue).MCvScalar, 2);


            var bmp = lineImage.Bitmap;

            bmp.Save("output_Lines.jpg", ImageFormat.Jpeg);


            var sourcePoints = new PointF[]
            {
                new PointF(0, 0), new PointF(0, image.Height), new PointF(image.Width, 0),
                new PointF(image.Width, image.Height)
            };

            var targetPoints = new PointF[] {points[0], points[1], points[2], points[3]};

            var transform = CvInvoke.GetPerspectiveTransform(targetPoints, sourcePoints);

            var outputImage = new Mat();

            CvInvoke.WarpPerspective(image, outputImage, transform, image.Size);

            outputImage.Bitmap.Save("Output_Transformed.jpg", ImageFormat.Jpeg);
            

        }

        List<PointF> _findPoints(LineSegment2D[] lines)
        {

            var lowXy = int.MaxValue;
            var lowXyLine = default(LineSegment2D);
            var lowXy_point = default(PointF);

           
            var lowXHighY_X = int.MaxValue;
            var lowXHighY_Y = 0;
            var lowXHighY_Line = default(LineSegment2D);
            var lowXHighY_point = default(PointF);

            var highXLowY_X = 0;
            var highXLowY_Y = int.MaxValue;
            var highXLowY_Line = default(LineSegment2D);
            var highXLowY_point = default(PointF);

            var highXHighY_X = 0;
            var highXHighY_Y = 0;
            var highXHighY_Line = default(LineSegment2D);
            var highXHighY_point = default(PointF);


            foreach (var line in lines)
            {
                
                //find low x and low y
                if (line.P1.X + line.P1.Y < lowXy - 5)
                {
                    lowXyLine = line;
                    lowXy = line.P1.X + line.P1.Y;
                    lowXy_point = new PointF(line.P1.X, line.P1.Y);
                }

                if (line.P2.X + line.P2.Y < lowXy - 5)
                {
                    lowXyLine = line;
                    lowXy = line.P2.X + line.P2.Y;
                    lowXy_point = new PointF(line.P1.X, line.P1.Y);
                }

                //find low x and High Y
                if (line.P1.X < lowXHighY_X && line.P1.Y > lowXHighY_Y)
                {
                    lowXHighY_X = line.P1.X;
                    lowXHighY_Y = line.P1.Y;
                    lowXHighY_Line = line;
                    lowXHighY_point = new PointF(line.P1.X, line.P1.Y);
                }

                if (line.P2.X < lowXHighY_X && line.P2.Y > lowXHighY_Y)
                {
                    lowXHighY_X = line.P2.X;
                    lowXHighY_Y = line.P2.Y;
                    lowXHighY_Line = line;
                    lowXHighY_point = new PointF(line.P1.X, line.P1.Y);
                }

                if (line.P1.X > highXLowY_X && line.P1.Y < highXLowY_Y)
                {
                    highXLowY_X = line.P1.X;
                    highXLowY_Y = line.P1.Y;
                    highXLowY_Line = line;
                    highXLowY_point = new PointF(line.P1.X, line.P1.Y);
                }

                if (line.P2.X > highXLowY_X && line.P2.Y < highXLowY_Y)
                {
                    highXLowY_X = line.P2.X;
                    highXLowY_Y = line.P2.Y;
                    highXLowY_Line = line;
                    highXLowY_point = new PointF(line.P1.X, line.P1.Y);
                }

                if (line.P1.X > highXHighY_X && line.P1.Y > highXHighY_Y)
                {
                    highXHighY_X = line.P1.X;
                    highXHighY_Y = line.P1.Y;
                    highXHighY_Line = line;
                    highXHighY_point = new PointF(line.P1.X, line.P1.Y);
                }

                if (line.P2.X > highXHighY_X && line.P2.Y > highXHighY_Y)
                {
                    highXHighY_X = line.P2.X;
                    highXHighY_Y = line.P2.Y;
                    highXHighY_Line = line;
                    highXHighY_point = new PointF(line.P1.X, line.P1.Y);
                }


            }

            return new List<PointF> { lowXy_point, lowXHighY_point, highXLowY_point, highXHighY_point };

            //return corners;

        }

        List<LineSegment2D> _findCorners(LineSegment2D[] lines)
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

            var corners = new List<LineSegment2D>();


            foreach (var line in lines)
            {
                //find lines that start or end near the start or end of this line and that have a sufficiently differnt rotation. 

                foreach (var lineInner in lines)
                {
                    if (lineInner.P1 == line.P1 && lineInner.P2 == line.P2)
                    {
                        continue;
                    }

                    if (_isNear(line, lineInner))
                    {
                        corners.Add(line);
                    }

                }

                //var lowerX = _findLower(line.P1.X, line.P2.X);

                //if (lowerX < leftX)
                //{
                //    leftX = lowerX;
                //}

                //var upperX = _findUpper(line.P1.X, line.P2.X);

                //if (upperX > rightX)
                //{
                //    rightX = upperX;
                //}

                //var upperY = _findUpper(line.P1.Y, line.P2.Y);
                ////upper as in up the image, so lower value :/
                //if (upperY < topY)
                //{
                //    topY = upperY;
                //}

                //var lowerY = _findLower(line.P1.Y, line.P2.Y);

                //if (lowerY > bottomY)
                //{
                //    bottomY = lowerY;
                //}
            }
            return corners;

        }

        bool _isNear(LineSegment2D line, LineSegment2D line2)
        {
            Debug.WriteLine($"Direction X: {line.Direction.X}, Direction Y: {line.Direction.Y}");

            var isNear = _isNearPoint(line.P1, line2.P1) || _isNearPoint(line.P2, line2.P2) || _isNearPoint(line.P1, line2.P2);

            if (!isNear)
            {
                return false;
            }

            return !_isNearRotation(line.Direction, line2.Direction);
        }

        private int nearness = 5;
        private int rotationNess = 10;

        bool _isNearRotation(PointF a, PointF b)
        {
            var dangleInRadians = Math.Atan2(1, 1) - Math.Atan2(a.Y, a.X);
            var dangleInRadians2 = Math.Atan2(1, 1) - Math.Atan2(b.Y, b.X);

            var isNearRotation = (dangleInRadians > dangleInRadians2 && dangleInRadians - dangleInRadians2 < rotationNess) ||
                 (dangleInRadians2 > dangleInRadians && dangleInRadians2 - dangleInRadians < rotationNess);

            return isNearRotation;
        }

        bool _isNearPoint(Point p1, Point p2)
        {
            return _isNearPoint(p1.X, p2.X) ||
                   _isNearPoint(p1.X, p2.Y) ||
                   _isNearPoint(p1.Y, p2.X) ||
                   _isNearPoint(p1.Y, p2.Y);
        }

        bool _isNearPoint(int p1, int p2)
        {
            if ((p1 > p2 && p1 - p2 < nearness) || (p2 > p1 && p2 - p1 < nearness))
            {
                return true;
            }

            return false;
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

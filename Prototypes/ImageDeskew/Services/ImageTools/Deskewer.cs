using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;

namespace Services
{
    public static class MatHelper
    {
        public static double GetDoubleValue(this Mat mat, int row, int col)
        {
            var value = new double[1];
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

        public static byte GetByteValue(this Mat mat, int row, int col)
        {
            var value = new byte[1];
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

        public static dynamic GetValue(this Mat mat, int row, int col)
        {
            var value = CreateElement(mat.Depth);

            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

        private static dynamic CreateElement(DepthType depthType)
        {
            if (depthType == DepthType.Cv8S)
            {
                return new sbyte[1];
            }
            if (depthType == DepthType.Cv8U)
            {
                return new byte[1];
            }
            if (depthType == DepthType.Cv16S)
            {
                return new short[1];
            }
            if (depthType == DepthType.Cv16U)
            {
                return new ushort[1];
            }
            if (depthType == DepthType.Cv32S)
            {
                return new int[1];
            }
            if (depthType == DepthType.Cv32F)
            {
                return new float[1];
            }
            if (depthType == DepthType.Cv64F)
            {
                return new double[1];
            }
            return new float[1];
        }
    }

    public class Deskewer
    {


        public (Bitmap, Bitmap, Bitmap, Bitmap, Bitmap) Deskew(Bitmap source, double cannyThreshold, double cannyThresholdLinking,
            int smooth = 31,
            double distanceResolution = 1,
            int threshold = 20,
            double angleResolution = 45.0,
            double minLineWidth = 30,
            double lineGap = 10,
            double hue = 10
            )
        {
            var origImage = new Image<Bgr, byte>(source);


            Image<Gray, byte> image = new Image<Gray, byte>(source);

            var imgSmooth = origImage.SmoothMedian(smooth);

            

            

            //UMat cleanedThreshold = new UMat();
            //CvInvoke.AdaptiveThreshold(image, cleanedThreshold, 255, Emgu.CV.CvEnum.AdaptiveThresholdType.MeanC,
            //    Emgu.CV.CvEnum.ThresholdType.Binary, 71, 35);



            //            kernel1 = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
            //close = cv2.morphologyEx(gray, cv2.MORPH_CLOSE, kernel1)
            //div = np.float32(gray) / (close)
            //res = np.uint8(cv2.normalize(div, div, 0, 255, cv2.NORM_MINMAX))

            //Image<Hsv, Byte> hsvimg = imgSmooth.Convert<Hsv, Byte>();

            ////extract the hue and value channels
            //Image<Gray, Byte>[] channels = hsvimg.Split();  //split into components
            //Image<Gray, Byte> imghue = channels[0];            //hsv, so channels[0] is hue.
            //Image<Gray, Byte> imgval = channels[2];            //hsv, so channels[2] is value.

            //filter out all but "the color you want"...seems to be 0 to 128 ?
            //Image<Gray, byte> huefilter = imghue.InRange(new Gray(hue), new Gray(hue + 5));



            //UMat cornerHarris = new UMat();
            //CvInvoke.CornerHarris(image, cornerHarris, 10, 3);

            //CvInvoke.Threshold(cornerHarris, cornerHarris, 0.0001,
            //        255.0, Emgu.CV.CvEnum.ThresholdType.BinaryInv);




            //imgSmooth.Bitmap.Save("Smooth.jpg", ImageFormat.Jpeg);

            Mat cannyEdges = new Mat();
            CvInvoke.Canny(imgSmooth, cannyEdges, cannyThreshold, cannyThresholdLinking);

            Mat morphologyOut = new Mat();

            var kernel1 = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(3, 3), new Point(-1, -1));
            var kernel2 = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(4, 4), new Point(-1, -1));

            //CvInvoke.MorphologyEx(cannyEdges, morphologyOut, MorphOp.Open, kernel1, new Point(-1, -1), 1,
            //    Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());

            CvInvoke.MorphologyEx(cannyEdges, morphologyOut, MorphOp.Close, kernel2, new Point(-1, -1), 1,
                Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());




            //Mat kernelOp = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(5, 5), new Point(-1, -1));
            //CvInvoke.MorphologyEx(cannyEdges, morphologyOut, MorphOp.Open, kernelOp, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

            //// Closing (dilate -> erode) para juntar regiones blancas.
            //Mat kernelCl = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(8, 8), new Point(-1, -1));
            //CvInvoke.MorphologyEx(morphologyOut, morphologyOut, MorphOp.Close, kernelCl, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());


            //LineSegment2D[] lines = CvInvoke.HoughLinesP(
            //  morphologyOut,
            //  distanceResolution, //Distance resolution in pixel-related units
            //  Math.PI / angleResolution, //Angle resolution measured in radians.
            //  threshold, //threshold
            //  minLineWidth, //min Line width
            //  lineGap); //gap between lines

            //double[] angle = new double[lines.Length];

            //for (int i = 0; i < lines.Length; i++)
            //{
            //    var dangleInRadians = Math.Atan2(1, 1) - Math.Atan2(lines[i].Direction.X, lines[i].Direction.Y);
            //    // double result = (double)(lines[i].P2.Y - lines[i].P1.Y) / (lines[i].P2.X - lines[i].P1.X);
            //    angle[i] = dangleInRadians;
            //}
            //double avg = 0;
            //for (int i = 0; i < lines.Length; i++)
            //{
            //    avg += angle[i];
            //}
            //avg = avg / lines.Length;
            //Gray g = new Gray(255);
            //Image<Gray, byte> imageRotate = image.Rotate(-avg, g);






            var pointsAll = new List<Point>();

            for (var r = 0; r < morphologyOut.Rows; r+=2)
            {
                for (var c = 0; c < morphologyOut.Cols; c+=2)
                {
                    var val = morphologyOut.GetByteValue(r, c);
                    if (val != 0)
                    {
                        pointsAll.Add(new Point(c, r));
                        var two = val;
                    }
                }
            }
            //out2.Save("output_Canny.jpg", ImageFormat.Jpeg);

           

            Mat lineImage = new Mat(image.Size, DepthType.Cv8U, 3);
            //Mat lineImage = new Mat(image.Size, DepthType.Cv8U, 3);
            //lineImage.SetTo(new MCvScalar(0));
            //foreach (LineSegment2D line in lines)
            //    CvInvoke.Line(lineImage, line.P1, line.P2, _randColor(), 2);

            //var points = _findPoints(lines);
            var points2 = _findPointsFromXY(pointsAll, image.Width, image.Height);

            //new Bgr(Color.Red).MCvScalar
            foreach (var p in points2)
            {
                CvInvoke.Circle(lineImage, new Point((int)p.X, (int)p.Y), 10, new Bgr(Color.Red).MCvScalar, 2);
            }

            foreach (var p in pointsAll)
            {
                CvInvoke.Circle(lineImage, p, 2, new Bgr(Color.CornflowerBlue).MCvScalar, 2);
            }


            // var bmp = lineImage.Bitmap;

            // bmp.Save("output_Lines.jpg", ImageFormat.Jpeg);


            var sourcePoints = new PointF[]
            {
                new PointF(0, 0), new PointF(0, image.Height), new PointF(image.Width, 0),
                new PointF(image.Width, image.Height)
            };

            var targetPoints = new PointF[] { points2[0], points2[1], points2[2], points2[3] };

            var transform = CvInvoke.GetPerspectiveTransform(targetPoints, sourcePoints);

            var outputImage = new Mat();

            CvInvoke.WarpPerspective(origImage, outputImage, transform, image.Size);

            outputImage.Bitmap.Save("Output_Transformed.jpg", ImageFormat.Jpeg);

            return (outputImage.Bitmap, imgSmooth.Bitmap, cannyEdges.Bitmap, lineImage.Bitmap, morphologyOut.Bitmap);
        }

        MCvScalar _randColor()
        {
            var ran = new Random((int)DateTime.Now.Ticks);

            return new MCvScalar(ran.Next(255), ran.Next(255), ran.Next(255), 0);
        }


        List<Point> _findPointsFromXY(List<Point> points, int width, int height)
        {
            var topLeft = new Point(0, 0);
            var bottomLeft = new Point(0, height);
            var topRight = new Point(width, 0);
            var bottomRight = new Point(width, height);

            double bestDistanceTopLeft = Double.MaxValue;
            double bestDistanceBottomLeft = Double.MaxValue;
            double bestDistanceTopRight = Double.MaxValue;
            double bestDistanceBottomRight = Double.MaxValue;

            Point bestTopLeft = default(Point);
            Point bestBottomLeft = default(Point);
            Point bestTopRight = default(Point);
            Point bestBottomRight = default(Point);

            foreach (var point in points)
            {
                var distanceTopLeft = Math.Sqrt((Math.Pow(point.X - topLeft.X, 2) + Math.Pow(point.Y - topLeft.Y, 2)));
                var distanceBottomLeft = Math.Sqrt((Math.Pow(point.X - bottomLeft.X, 2) + Math.Pow(point.Y - bottomLeft.Y, 2)));
                var distanceTopRight = Math.Sqrt((Math.Pow(point.X - topRight.X, 2) + Math.Pow(point.Y - topRight.Y, 2)));
                var distanceBottomRight = Math.Sqrt((Math.Pow(point.X - bottomRight.X, 2) + Math.Pow(point.Y - bottomRight.Y, 2)));


                if (distanceTopLeft < bestDistanceTopLeft)
                {
                    bestDistanceTopLeft = distanceTopLeft;
                    bestTopLeft = point;
                }

                if (distanceBottomLeft < bestDistanceBottomLeft)
                {
                    bestDistanceBottomLeft = distanceBottomLeft;
                    bestBottomLeft = point;
                }

                if (distanceTopRight < bestDistanceTopRight)
                {
                    bestDistanceTopRight = distanceTopRight;
                    bestTopRight = point;
                }

                if (distanceBottomRight < bestDistanceBottomRight)
                {
                    bestDistanceBottomRight = distanceBottomRight;
                    bestBottomRight = point;
                }

                //double distanceSquared = (point - topLeft).LengthSquared;
                ////double distanceTopLeft = Point.Subtract(point, topLeft).Length;

                ////find low x and low y
                //if (point.X + point.Y < lowXy)
                //{
                //    lowXy = point.X + point.Y;
                //    lowXy_point = point;
                //}

                //if (point.X < lowXHighY_X && point.Y > lowXHighY_Y)
                //{
                //    lowXHighY_X = point.X;
                //    lowXHighY_Y = point.Y;

                //    lowXHighY_point = point;
                //}

                //if (point.X > highXLowY_X-10 && point.Y < highXLowY_Y)
                //{
                //    highXLowY_X = point.X;
                //    highXLowY_Y = point.Y;

                //    highXLowY_point = point;
                //}

                //if (point.X > highXHighY_X && point.Y > highXHighY_Y)
                //{
                //    highXHighY_X = point.X;
                //    highXHighY_Y = point.Y;

                //    highXHighY_point = point;
                //}
            }

            return new List<Point> { bestTopLeft, bestBottomLeft, bestTopRight, bestBottomRight };
            //if (point.X > highXLowY_X && point.Y < highXLowY_Y)
            //{
            //    highXLowY_X = point.X;
            //    highXLowY_Y = point.Y;

            //    highXLowY_point = point;
            //}

            //if (point.X > highXHighY_X && point.Y > highXHighY_Y)
            //{
            //    highXHighY_X = point.X;
            //    highXHighY_Y = point.Y;

            //    highXHighY_point = point;
            //}






            //foreach (var line in lines)
            //{
            //    if (line.P1.X < lowXy_point.X || line.P1.Y < lowXy_point.Y ||
            //        line.P2.X < lowXy_point.X || line.P2.Y < lowXy_point.Y)
            //    {
            //        Debug.WriteLine("Here");
            //    }
            //}


            //return new List<PointF> { lowXy_point, lowXHighY_point, highXLowY_point, highXHighY_point };

            //return corners;

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
                if (line.P1.X + line.P1.Y < lowXy)
                {
                    lowXyLine = line;
                    lowXy = line.P1.X + line.P1.Y;
                    lowXy_point = new PointF(line.P1.X, line.P1.Y);
                }

                if (line.P2.X + line.P2.Y < lowXy)
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

            foreach (var line in lines)
            {
                if (line.P1.X < lowXy_point.X || line.P1.Y < lowXy_point.Y ||
                    line.P2.X < lowXy_point.X || line.P2.Y < lowXy_point.Y)
                {
                    Debug.WriteLine("Here");
                }
            }


            return new List<PointF> { lowXy_point, lowXHighY_point, highXLowY_point, highXHighY_point };

            //return corners;

        }
    }
}

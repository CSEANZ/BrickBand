using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;

namespace Services
{
    public class Deskewer
    {
        public (Bitmap, Bitmap, Bitmap, Bitmap) Deskew(Bitmap source, double cannyThreshold, double cannyThresholdLinking)
        {
            var origImage = new Image<Bgr, byte>(source);

            Image<Gray, byte> image = new Image<Gray, byte>(source);

            var imgSmooth = image.SmoothMedian(31);
            
            //imgSmooth.Bitmap.Save("Smooth.jpg", ImageFormat.Jpeg);

            UMat cannyEdges = new UMat();
            CvInvoke.Canny(imgSmooth, cannyEdges, cannyThreshold, cannyThresholdLinking);

            //out2.Save("output_Canny.jpg", ImageFormat.Jpeg);

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


           // var bmp = lineImage.Bitmap;

           // bmp.Save("output_Lines.jpg", ImageFormat.Jpeg);


            var sourcePoints = new PointF[]
            {
                new PointF(0, 0), new PointF(0, image.Height), new PointF(image.Width, 0),
                new PointF(image.Width, image.Height)
            };

            var targetPoints = new PointF[] { points[0], points[1], points[2], points[3] };

            var transform = CvInvoke.GetPerspectiveTransform(targetPoints, sourcePoints);

            var outputImage = new Mat();

            CvInvoke.WarpPerspective(origImage, outputImage, transform, image.Size);

            outputImage.Bitmap.Save("Output_Transformed.jpg", ImageFormat.Jpeg);

            return (outputImage.Bitmap, imgSmooth.Bitmap, cannyEdges.Bitmap, lineImage.Bitmap);
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
    }
}

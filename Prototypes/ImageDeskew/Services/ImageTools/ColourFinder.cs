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
        public Bitmap VisualiseZones(ColourBoardProfile zones)
        {
            Mat lineImage = new Mat(new Size(1024, 400), DepthType.Cv8U, 3);

            var pixPerCol = lineImage.Width / zones.Zones.Count;

           // CvInvoke.Rectangle(lineImage, new Rectangle(0, 0, lineImage.Width, lineImage.Height), new Bgr(zones.BaseColour.B, zones.BaseColour.G, zones.BaseColour.R).MCvScalar, 2000);

            for (var z = 0; z < zones.Zones.Count; z++)
            {
                
                var zone = zones.Zones[z];
                var pixelOffset = ((z + 1) * pixPerCol) / 2;
                if (_nearEnoughCompare(zone.Average, zones.BaseColour.Average) || zone.Average == 0)
                {
                    //this is a bg one!
                    CvInvoke.Rectangle(lineImage, new Rectangle(pixelOffset, lineImage.Height / 2, 2, 2), new Bgr(Color.White).MCvScalar, 2);
                    continue;
                }

                   
                CvInvoke.Circle(lineImage, new Point(pixelOffset, lineImage.Height / 2), 10, new Bgr(zone.B, zone.G, zone.R).MCvScalar, 2);
            }

            return lineImage.Bitmap;
        }


        public ColourBoardProfile CompareColours(ColourBoardProfile calibration, ColourBoardProfile compare)
        {
            var lActuals = new List<ColourZone>();
            foreach (var compCol in compare.Zones)
            {
                ColourZone currentColorZone = null;

                if (_nearEnoughCompare(compCol.Average, calibration.BaseColour.Average))
                {
                    //this is a base background color, ignore it. 
                    lActuals.Add(new ColourZone());
                    continue;
                }

                foreach (var calCol in calibration.Zones)
                {
                    if (compCol.Average >= calCol.Average - 5 && compCol.Average <= calCol.Average + 5)
                    {
                        currentColorZone = calCol;
                    }
                }

                lActuals.Add(currentColorZone ?? new ColourZone());
            }

            var result = new ColourBoardProfile
            {
                BaseColour = calibration.BaseColour,
                Zones = lActuals
            };

            return result;
        }


        public ColourBoardProfile FindColors(Bitmap bm, bool calibration, int columns = 17)
        {
            var origImage = new Image<Bgr, byte>(bm);

            var imgSmooth = origImage.SmoothMedian(41);

            Image<Hsv, Byte> hsvimg = imgSmooth.Convert<Hsv, Byte>();

            var byteList = new List<Tuple<int, int, Bgr>>();


            var pixPerCol = (hsvimg.Cols / columns);

            var colAverage = new Dictionary<int, Tuple<int, int, int, int, Bgr>>(); //count, totalb, totalg, totalr, pixelaverage

            for (var r = (hsvimg.Rows / 2) - 5; r < (hsvimg.Rows / 2) + 5; r += 1)
            {
                for (var c = 0; c < hsvimg.Cols; c += 1)
                {
                    var pixel = origImage[r, c];
                    byteList.Add(new Tuple<int, int, Bgr>(r, c, pixel));



                    var thisCol = (int)(c / pixPerCol);

                    var upper = (thisCol + 1) * pixPerCol;
                    var lower = thisCol * pixPerCol;

                    if (c < lower + 10 || c > upper - 10)
                    {
                        continue;
                    }

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

            for (var c = 0; c < columns; c++)
            {
                var thisColData = colAverage[c];

                var pixelOffset = ((c + 1) * pixPerCol) / 2;

                CvInvoke.Circle(lineImage, new Point(pixelOffset, origImage.Height / 2), 20, thisColData.Item5.MCvScalar, 2);
            }

            var listOfColumns = new List<ColourZone>();

            var _colorInstanceCount = new Dictionary<int, int>();

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

                var existing = _findCompare(_colorInstanceCount.Keys.ToList(), cz.Average);
                if (existing == -1)
                {
                    _colorInstanceCount.Add(cz.Average, 1);
                }
                else
                {
                    _colorInstanceCount[existing]++;
                }

                listOfColumns.Add(cz);

            }


            var most = _colorInstanceCount.OrderByDescending(_ => _.Value);
            var mostItem = most.First().Key;

            var boardProfile = new ColourBoardProfile
            {
                Zones = new List<ColourZone>()
            };

            foreach (var item in listOfColumns)
            {
                if (_nearEnoughCompare(item.Average, mostItem))
                {
                    if (calibration)
                    {
                        if (boardProfile.BaseColour == null)
                        {
                            boardProfile.BaseColour = item;
                        }
                    }
                    else
                    {
                        item.IsBase = true;
                        boardProfile.Zones.Add(item);
                    }
                   
                }
                else
                {
                    boardProfile.Zones.Add(item);
                }
            }

            var json = JsonConvert.SerializeObject(listOfColumns);

            File.WriteAllText("calibration.json", json);

            //foreach (var p in byteList)
            //{
            //    CvInvoke.Circle(lineImage, new Point(p.Item2, p.Item1), 2, p.Item3.MCvScalar, 2);
            //}

            //lineImage.Bitmap.Save("Output_ColorTester.jpg", ImageFormat.Jpeg);

            return boardProfile;

        }

        int _findCompare(List<int> items, int b)
        {
            foreach (var item in items)
            {
                if (_nearEnoughCompare(item, b))
                {
                    return item;
                }
            }

            return -1;
        }

        bool _nearEnoughCompare(int a, int b)
        {
            return a >= b - 3 && a <= b + 3;
        }
    }
}


using System.IO;
using PanoBeamLib;
using System.Windows;
using PanoBeam.Configuration;

namespace PanoBeamDebug
{
    class Program
    {
        static void Main(string[] args)
        {
            //new Program().DetectSurface();
            //new Program().DetectShapes();
            //new Program().WarpTest();
            new Program().Detect();
        }

        public void Detect()
        {
            var screen = new PanoScreen
            {
                Resolution = new System.Drawing.Size(3240, 1080),
                Overlap = 600,
            };
            screen.AddProjectors(0, 1);
            screen.SetPattern(80, new System.Drawing.Size(10, 7), false, false);

            screen.ClippingRectangle = new System.Drawing.Rectangle(25, 26, 1849, 841);

            screen.Detect();

        }

        public void WarpTest()
        {
            /*var res = PanoBeamLib.Helpers.TestWarp();
            Console.WriteLine(res);
            Console.ReadKey();*/
        }

        /*public void DetectShapes()
        {
            int count = 40;
            int minSize = 5;
            int maxSize = 80;
            var image = (Bitmap)Image.FromFile(@"C:\Users\marco\Downloads\PanoBeam\capture_pattern0.png");
            Rectangle clippingRectangle = new Rectangle(new Point(563, 360), new Size(1156, 382));
            var clippingRectangleCorners = new[]
            {
                new AForge.IntPoint(clippingRectangle.X, clippingRectangle.Y),
                new AForge.IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y),
                new AForge.IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y + clippingRectangle.Height),
                new AForge.IntPoint(clippingRectangle.X, clippingRectangle.Y + clippingRectangle.Height)
            };
            Helpers.FillOutsideBlack(image, clippingRectangleCorners);

            var blobCounter = new AForge.Imaging.BlobCounter();
            AForge.Imaging.Blob[] blobs;
            blobCounter.FilterBlobs = true;
            blobCounter.MaxHeight = maxSize;
            blobCounter.MaxWidth = maxSize;
            blobCounter.MinHeight = minSize;
            blobCounter.MinWidth = minSize;

            var threshold = Recognition.GetThreshold(image);

            blobCounter.BackgroundThreshold = Color.FromArgb(255, threshold, threshold, threshold);
            blobCounter.ProcessImage(image);
            blobs = blobCounter.GetObjectsInformation();

        }

        public void DetectSurface()
        {
            var _bmpWhite = (Bitmap)Image.FromFile(Path.Combine(@"C:\Users\marco\Downloads\PanoBeam", "capture_white.png"));
            Rectangle clippingRectangle = new Rectangle(new Point(563, 360), new Size(1156, 382));
            var clippingRectangleCorners = new[]
            {
                new AForge.IntPoint(clippingRectangle.X, clippingRectangle.Y),
                new AForge.IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y),
                new AForge.IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y + clippingRectangle.Height),
                new AForge.IntPoint(clippingRectangle.X, clippingRectangle.Y + clippingRectangle.Height)
            };
            Helpers.FillOutsideBlack(_bmpWhite, clippingRectangleCorners);
            //SaveBitmap(_bmpWhite, Path.Combine(@"C:\Users\marco\Downloads\PanoBeam\123", "outsideblack.png"));

            var corners = Recognition.DetectSurface(_bmpWhite);
            corners = Calculations.SortCorners(corners);
            Helpers.SaveImageWithMarkers(_bmpWhite, corners, Path.Combine(@"C:\Users\marco\Downloads\PanoBeam\123", "detect_white.png"), 5);
        }

        private static void SaveBitmap(Bitmap bmp, string fileName)
        {
            var saveBitmap = (Bitmap)bmp.Clone();
            saveBitmap.Save(fileName);
        }*/
    }
}

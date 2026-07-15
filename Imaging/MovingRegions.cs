using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MED.Imaging
{
    public class MovingRegions : ImageProcess
    {
        public MovingRegions(string paramSection = "MovingRegions", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false)
            : base(paramSection, performance, formHandler, imageConsumer, isAynchrone)
        {
        }


        public override bool HasImageChanged
        {
            get
            {
                if (ImageProvider == null)
                    return base.HasImageChanged;
                else
                    return ImageProvider.HasImageChanged;
            }
            set
            {
                if (ImageProvider == null)
                    base.HasImageChanged = value;
                else
                    ImageProvider.HasImageChanged = base.HasImageChanged = value;
            }
        }

        public override void Run()
        {
            MoveDetectInit = false;

            base.Run();
        }

        public bool MoveDetectInit = false;
        public EDMovDetect MoveDetector;

        public override Bitmap Image
        {
            get
            {
                if (ImageProvider == null)
                    return null;

                if (!HasImageChanged)
                    return ImageProvider.Image;

                Bitmap image = ImageProvider.Image;
                if (image == null)
                    return null;

                bool shrink = false;
                if (shrink)
                {
                    float newWidth = 256F;
                    var size = new Size((int)newWidth, (int)(image.Height * newWidth / image.Width));
                    image = new Bitmap(image, size);
                }
                ImageProvider.Image = image;

                if (!MoveDetectInit)
                {

                    InitMoveDetector(image);

                    return image;
                }
                int x, y;

                Performance.Resume("Process MoveDetectorAction", true);

                MoveDetectorAction(image);

                Performance.Pause("done Process MoveDetectorAction");

                return image;

            }
            set { base.Image = value; }
        }


        /**
         * 
         */
        private void InitMoveDetector(Bitmap image)
        {
            if (MoveDetector == null)
                MoveDetector = new EDMovDetect();

            MoveDetector.get_Limites(0).SetPreselLight();
            MoveDetector.NbreZonesMove = 24;
            MoveDetector.Mvt = 2;
            if (image.Width > 160)
            {
                MoveDetector.ResolutionY = MoveDetector.ResolutionX = (byte)(image.Width / 160);
            }
            else
                MoveDetector.ResolutionY = 1;
            MoveDetector.SetNewImage(image, true);
            MoveDetectInit = true;
        }

        /**
         * 
         */
        private void MoveDetectorAction(Bitmap image)
        {
            MoveDetector.SetNewImage(image, false);

            Graphics gr = Graphics.FromImage(image);

            if (MoveDetector.GetZonesMove != null && MoveDetector.GetZonesMove.Count > 0)
            {

                Performance.Step($"Move Detection : {MoveDetector.GetZonesMove.Count}, {MoveDetector.RegionDetect.GetBounds(gr)}, {MoveDetector.get_Limites(0).RectAnalyse}");
                Performance.Step(MoveDetector.GetStats());

                SolidBrush brush = new SolidBrush(Color.Blue);
                foreach (ZoneMove zoneMove in MoveDetector.GetZonesMove)
                {
                    gr.FillRectangles(brush, zoneMove.Frame);
                }
            }
            if (!MoveDetector.RegionDetect.IsEmpty(gr))
            {

                Performance.Step($"move Detection : {MoveDetector.GetZonesMove.Count}, {MoveDetector.RegionDetect.GetBounds(gr)}");
                Pen pen = new(Color.Green);
                gr.DrawRectangle(pen, MoveDetector.RegionDetect.GetBounds(gr));
            }
            else
            {
                Performance.Step($"NO move Detection");
            }
            gr.Dispose();

        }
    }
}

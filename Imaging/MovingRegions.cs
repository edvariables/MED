using System;
using System.Collections.Generic;
using System.ComponentModel;
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


        [Browsable(false)]
        public override Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = base.ObjectsProperties;
                if (MoveDetector != null)
                {
                    dict.Add(this.Name + ".MoveDetector", MoveDetector);
                    for(int i= 0; i < MoveDetector.IdxLimites; i++)
                        dict.Add($"{this.Name}.MoveDetector.Limites{i}", MoveDetector.get_Limites(i));
                }

                return dict;
            }
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

        public override void Start()
        {
            MoveDetectInit = false;

            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
            IsRunning = true;
        }

        public bool MoveDetectInit = false;
        [Browsable(false)]
        public EDMovDetect MoveDetector { get; private set; }

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
                    ImageProvider.Image = image;
                }

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

            var zones = MoveDetector.GetZonesMove;

            if (zones != null && zones.Count > 0)
            {

                Performance.Step($"Move Detection : Zones : {MoveDetector.GetZonesMove.Count}, RegionDetect : {MoveDetector.RegionDetect.GetBounds(gr)}, Limites(0).RectAnalyse : {MoveDetector.get_Limites(0).RectAnalyse}");
                Performance.Step(MoveDetector.GetStats());

                SolidBrush brush = new SolidBrush(Color.Blue);
                foreach (ZoneMove zoneMove in MoveDetector.GetZonesMove)
                {
                    gr.FillRectangles(brush, zoneMove.Frame);
                }
            }
            if (!MoveDetector.RegionDetect.IsEmpty(gr))
            {
                if (zones != null && zones.Count > 0)
                    Performance.Step($"move Detection : No zone, RegionDetect : {MoveDetector.RegionDetect.GetBounds(gr)}");
                Pen pen = new(Color.Green);
                gr.DrawRectangle(pen, MoveDetector.RegionDetect.GetBounds(gr));
            }
            else if(zones == null || zones.Count == 0)
            {
                Performance.Step($"NO move Detection");
            }
            gr.Dispose();

        }
    }
}

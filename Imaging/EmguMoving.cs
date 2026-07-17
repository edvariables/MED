//using Emgu.CV;
//using Emgu.CV.Structure;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Drawing;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading;
//using static Emgu.CV.ML.KNearest;
//using static System.Runtime.InteropServices.JavaScript.JSType;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

//namespace MED.Imaging
//{
//    public class EmguMoving : ImageProcess
//    {
//        public EmguMoving(string paramSection = "EmguMoving", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false)
//            : base(paramSection, performance, formHandler, imageConsumer, isAynchrone)
//        {
//        }


//        [Browsable(false)]
//        public override Dictionary<string, object> ObjectsProperties
//        {
//            get
//            {
//                var dict = base.ObjectsProperties;
//                if (DISOpticalFlow != null)
//                {
//                    dict.Add(this.Name + ".DISOpticalFlow", DISOpticalFlow);
//                    //for (int i = 0; i < MoveDetector.IdxLimites; i++)
//                    //    dict.Add($"{this.Name}.MoveDetector.Limites{i}", MoveDetector.get_Limites(i));
//                }

//                return dict;
//            }
//        }

//        public override bool HasImageChanged
//        {
//            get
//            {
//                if (ImageProvider == null)
//                    return base.HasImageChanged;
//                else
//                    return ImageProvider.HasImageChanged;
//            }
//            set
//            {
//                if (ImageProvider == null)
//                    base.HasImageChanged = value;
//                else
//                    ImageProvider.HasImageChanged = base.HasImageChanged = value;
//            }
//        }

//        public override void Start()
//        {
//            MoveDetectInit = false;

//            base.Start();

//            ProcessState = System.Threading.ThreadState.Running;
//            IsRunning = true;
//        }

//        public bool MoveDetectInit = false;
//        [Browsable(false)]
//        public DISOpticalFlow DISOpticalFlow { get; private set; }

//        public override Bitmap Image
//        {
//            get
//            {
//                if (ImageProvider == null)
//                    return null;

//                if (!HasImageChanged)
//                    return ImageProvider.Image;

//                Bitmap image = ImageProvider.Image;
//                if (image == null)
//                    return null;

//                bool shrink = false;
//                if (shrink)
//                {
//                    float newWidth = 256F;
//                    var size = new Size((int)newWidth, (int)(image.Height * newWidth / image.Width));
//                    image = new Bitmap(image, size);
//                    ImageProvider.Image = image;
//                }

//                if (!MoveDetectInit)
//                {

//                    InitMoveDetector(image);

//                    return image;
//                }
//                int x, y;

//                Performance.Resume("Process MoveDetectorAction", true);

//                MoveDetectorAction(image);

//                Performance.Pause("done Process MoveDetectorAction");

//                return image;

//            }
//            set { base.Image = value; }
//        }

//        private Bitmap BackgroundImage;
//        /**
//         * 
//         */
//        private void InitMoveDetector(Bitmap image)
//        {
//            if (DISOpticalFlow == null)
//                DISOpticalFlow = new DISOpticalFlow(DISOpticalFlow.Preset.Fast);

//            BackgroundImage = image;

//            MoveDetectInit = true;
//        }

//        /**
//         * 
//         */
//        private void MoveDetectorAction(Bitmap image)
//        {
//            var flowx = new Image<Gray, float>(BackgroundImage.Size);
//            var flowy = new Image<Gray, float>(BackgroundImage.Size);

//            DISOpticalFlow.(currImg, prevImg, new Size(15, 15), flowx, flowy);
            
//            CvInvoke.cvAbsDiff(BackgroundImage.Convert<Gray, Byte>(),
//                     image.Convert<Gray, Byte>(), des);
//            CvInvoke.cvThreshold(des, thres, 20, 255, THRESH.CV_THRESH_BINARY);
//            CvInvoke.cvErode(thres, eroded, IntPtr.Zero, 2);

//            Graphics gr = Graphics.FromImage(image);

//            var zones = DISOpticalFlow.GetZonesMove;

//            if (zones != null && zones.Count > 0)
//            {

//                Performance.Step($"Move Detection : Zones : {DISOpticalFlow.GetZonesMove.Count}, RegionDetect : {DISOpticalFlow.RegionDetect.GetBounds(gr)}, Limites(0).RectAnalyse : {DISOpticalFlow.get_Limites(0).RectAnalyse}");
//                Performance.Step(DISOpticalFlow.GetStats());

//                SolidBrush brush = new SolidBrush(Color.Blue);
//                foreach (ZoneMove zoneMove in DISOpticalFlow.GetZonesMove)
//                {
//                    gr.FillRectangles(brush, zoneMove.Frame);
//                }
//            }
//            if (!DISOpticalFlow.RegionDetect.IsEmpty(gr))
//            {
//                if (zones != null && zones.Count > 0)
//                    Performance.Step($"move Detection : No zone, RegionDetect : {DISOpticalFlow.RegionDetect.GetBounds(gr)}");
//                Pen pen = new(Color.Green);
//                gr.DrawRectangle(pen, DISOpticalFlow.RegionDetect.GetBounds(gr));
//            }
//            else if (zones == null || zones.Count == 0)
//            {
//                Performance.Step($"NO move Detection");
//            }
//            gr.Dispose();

//        }
//    }
//}

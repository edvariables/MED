using DirectShowLib;
using DynamicData;
using Emgu.CV;
using MED.Imaging;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;

namespace MED.EDWebCam
{
    public partial class FWebCam : ImageProcessForm
    {
        public FWebCam()
        {
            InitializeComponent();
        }

        protected override void WndProc(ref Message m)
        {
            FormWindowState org = this.WindowState;
            base.WndProc(ref m);
            if (this.WindowState != org)
                this.FWebCam_WindowStateChanged(null, EventArgs.Empty);
        }

        private void FWebCam_Load(object sender, EventArgs e)
        {
            Init_AvailableCameras();

            Initialize_Logger();

            InitializeProcesses();

            LoadSettings();

        }

        #region Form
        private void FWebCam_Activated(object sender, EventArgs e)
        {
            List<object> objects = new();
            foreach (var process in Processes)
                objects.AddRange(process.ObjectsProperties.Values);
            if (Processes.Count > 0)
                objects.Add(this.Performance);
            FProperties.CurrentProperties = objects.ToArray();
        }

        private void FWebCam_WindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
                MdiParent = null;
            else
            {
                MdiParent = FStudio.Current;
                if (WindowState == FormWindowState.Normal)
                    Dock = DockStyle.Fill;
            }
        }

        Size DockedSize;
        private void FWebCam_DockChanged(object sender, EventArgs e)
        {
            if (this.Dock == DockStyle.Fill)
                DockedSize = this.Size;
            else if (!DockedSize.IsEmpty)
                this.Size = DockedSize;

        }
        #endregion

        private CheckBox chkLogColored;
        private CheckBox chkVideoCaptureLogger;
        private CheckBox chkRenderLogger;
        private void Initialize_Logger()
        {
            chkLogColored = (CheckBox)FLogger.Current.Controls.Find("chkLogColored", true).First();
            chkVideoCaptureLogger = (CheckBox)FLogger.Current.Controls.Find("chkVideoCaptureLogger", true).First();
            chkRenderLogger = (CheckBox)FLogger.Current.Controls.Find("chkRenderLogger", true).First();

            chkVideoCaptureLogger.CheckedChanged += chkVideoCaptureLogger_CheckedChanged;
            chkRenderLogger.CheckedChanged += chkRenderLogger_CheckedChanged;
            chkLogColored.CheckedChanged += chkLogColoredNot_CheckedChanged;
        }


        #region Settings

        public override void LoadSettings(bool loadChildren = false)
        {
            base.LoadSettings(loadChildren);

            chkRenderLogger.Checked = Render.Performance.Enabled;
            chkVideoCaptureLogger.Checked = ImageSource.Performance.Enabled;

            var value = ImageSource.ImageSizeMax;
            if (ImageSource.ImageSizeMax.IsEmpty)
                cboCaptureSize.Text = "";
            else
                cboCaptureSize.Text = Core.Parser.SizeToPretty(ImageSource.ImageSizeMax);
        }
        #endregion

        private void Init_AvailableCameras()
        {
            cboCameras.Items.Clear();
            foreach (var cam in EDVideoCapture.AvailableCameras())
                cboCameras.Items.Add(cam);
            if (cboCameras.Items.Count > 0)
                cboCameras.SelectedIndex = 0;
        }

        /**
         * 
         * 
         */
        #region Processes

        EDVideoCapture ImageSource;
        Render Render;
        /**
         * InitializeProcesses
         * 
         * */
        protected override void InitializeProcesses(bool resetAll = false)
        {
            base.InitializeProcesses(resetAll);

            FLogger.Current.Logger.Clear();

            if (Processes != null && Processes.Count > 0 && !resetAll)
                return;

            if (Performance == null || resetAll)
            {
                Performance = new(this.Name, FLogger.Current.Logger);
            }

            //Render
            if (Render == null || resetAll)
            {
                Render = new Render(
                    "Render"
                    , Performance.Sub("Render", chkRenderLogger.Checked, KnownColor.Yellow)
                    , picRender
                );
                Render.Performance.IsColored = chkLogColored.Checked;
            }
            else
            {
                //ImageProcess.Stop() kills OnImageChanged register
                Render.OnImageChanged += this.ImageChanged;
            }

            //Add process
            Processes.Add(Render);

            ImageProcess imgProc;

            //MovingRegions
            var EmguMoving = new EmguMoving(
                "EmguMoving"
                , Performance.Sub("EmguMoving", chkRenderLogger.Checked, KnownColor.AliceBlue)
                , picRender
                , (ImageProcess)Processes.Last()
            );

            //Add process
            Processes.Add(EmguMoving);

            //ScreenSplitter
            var ScreenSplitter = new ScreenSplitter(
                "ScreenSplitter"
                , Performance.Sub("ScreenSplitter", chkRenderLogger.Checked, KnownColor.GreenYellow)
                , picRender
                , (ImageProcess)Processes.Last()
            );

            //Add process
            Processes.Add(ScreenSplitter);

            //WebCam
            if (ImageSource == null || resetAll)
            {
                ImageSource = new EDVideoCapture(
                    "WebCam"
                    , Performance.Sub("WebCam", chkVideoCaptureLogger.Checked, FLogger.Current.DefaultLoggerColor.ToKnownColor())
                    , picRender
                    , (IImageConsumer)Processes.Last()
                );
                ImageSource.Performance.IsColored = chkLogColored.Checked;
                ImageSource.ProcessStateChanged += WebCam_ProcessStateChanged;
            }
            else
            {
                //ImageProcess.Stop() kills OnImageChanged register
                ImageSource.OnImageChanged += (Processes.Last() as IImageConsumer).ImageChanged;
            }


            ImageSource.OnFrameChanged = null;
            ImageSource.OnFrameChanged += EmguMoving.FrameChanged;

            ImageSource.OnImageChanged = null;
            ImageSource.OnImageChanged += ScreenSplitter.ImageChanged;

            EmguMoving.OnImageChanged = null;
            EmguMoving.OnImageChanged += ScreenSplitter.ImageChanged;

            ScreenSplitter.OnImageChanged = null;
            ScreenSplitter.OnImageChanged += Render.ImageChanged;

            Render.OnImageChanged = null;
            Render.OnImageChanged += this.ImageChanged;

            //WebCam.ImageSizeMax
            if (cboCaptureSize.Text == "")
            {
                if (!ImageSource.ImageSizeMax.IsEmpty)
                    cboCaptureSize.Text = Core.Parser.SizeToPretty(ImageSource.ImageSizeMax);
            }
            else
                ImageSource.ImageSizeMax = Core.Parser.SizeFromPretty(cboCaptureSize.Text);
            //Add process
            Processes.Add(ImageSource);

            foreach (var proc in Processes)
                (proc as Process).LoadSettings();
        }
        #endregion

        #region Process
        private void WebCam_ProcessStateChanged(IProcess sender, System.Threading.ThreadState state)
        {
            if (sender.IsRunning)
            {
                //chkRun.Checked = true;
                //if (state == ThreadState.Running)
                //    chkRun.Text = "En cours...";
                //else if (state == ThreadState.Suspended)
                //    chkRun.Text = "Pause";

            }
            else
                Stop();
        }

        /**
         * 
         * 
         */
        public override void Start()
        {
            base.Start();

            FLogger.Current.Start();

            foreach (var item in Processes.ToArray().Reverse())
            {
                if (item == ImageSource)
                {
                    ImageSource.CameraIndex = cboCameras.SelectedIndex;
                }
                item.Start();
            }

            ProcessState = ThreadState.Running;
        }

        /**
         * 
         * 
         */
        public override void Stop()
        {

            if (!IsRunning)
                return;

            base.Stop();

            FLogger.Current.Stop();

            ProcessState = ThreadState.Stopped;
        }
        #endregion
        /**
         * Image
         * */
        public override void ImageChanged(IImageProvider sender, EventArgs e)
        {
            if (this.Disposing || this.IsDisposed)
                return;
            base.ImageChanged(sender, e);

            FLogger.Current.RefreshProgress((ImageProcess)sender);

            FLogger.Current.ProgressMessage = $"Webcam [{ImageSource.Performance.Counter}]";
        }


        private void chkRenderLogger_CheckedChanged(object sender, EventArgs e)
        {
            if (Render != null)
                Render.Performance.Enabled = chkRenderLogger.Checked;
        }
        private void chkVideoCaptureLogger_CheckedChanged(object sender, EventArgs e)
        {
            if (ImageSource != null)
                ImageSource.Performance.Enabled = chkVideoCaptureLogger.Checked;
        }
        private void chkLogColoredNot_CheckedChanged(object sender, EventArgs e)
        {
            if (Render == null)
                return;
            Performance.IsColored = chkLogColored.Checked;
        }

        private void cboCaptureSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCaptureSize.Text == "")
                ImageSource.ImageSizeMax = Size.Empty;
            else
                ImageSource.ImageSizeMax = Core.Parser.SizeFromPretty(cboCaptureSize.Text);
        }
    }
}

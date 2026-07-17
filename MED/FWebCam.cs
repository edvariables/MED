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
    public partial class FWebCam : Form, IImageConsumer, IProcess
    {
        public FWebCam()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Init_AvailableCameras();

            Initialize_Logger();

            Initialize_Objects();

            LoadSettings();

        }


        private void FWebCam_Activated(object sender, EventArgs e)
        {
            List<object> objects = new();
            foreach (var process in ImageProcesses)
                objects.AddRange(process.ObjectsProperties.Values);
            if (ImageProcesses.Count > 0)
                objects.Add(Performance);
            FProperties.CurrentProperties = objects.ToArray();
        }

        protected override void WndProc(ref Message m)
        {
            FormWindowState org = this.WindowState;
            base.WndProc(ref m);
            if (this.WindowState != org)
                this.FWebCam_WindowStateChanged(null, EventArgs.Empty);
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

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        [Browsable(true)]
        List<ImageProcess> ImageProcesses { get; set; }
        EDVideoCapture WebCam;
        Render Render;

        #region Settings

        private void LoadSettings()
        {
            Core.Settings.ClearCache(true, true, this.Name);

            chkRenderLogger.Checked = Render.Performance.Enabled;
            chkVideoCaptureLogger.Checked = WebCam.Performance.Enabled;

            var value = WebCam.ImageSizeMax;
            if (WebCam.ImageSizeMax.IsEmpty)
                cboCaptureSize.Text = "";
            else
                cboCaptureSize.Text = Core.Parser.SizeToPretty(WebCam.ImageSizeMax);
        }
        public void SaveSettings()
        {
            Render.SaveSettings();
            WebCam.SaveSettings();
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

        Performance Performance;


        public bool IsRunning { get => ProcessState == ThreadState.Running || ProcessState == ThreadState.Suspended; }

        private ThreadState _ProcessState = ThreadState.Unstarted;
        public System.Threading.ThreadState ProcessState
        {
            get
            {
                if (WebCam == null)
                    return ProcessState;
                return WebCam.ProcessState;
            }
            set
            {
                if (WebCam == null)
                    _ProcessState = value;
                WebCam.ProcessState = value;
            }
        }

        private void chkRun_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRun.Checked)
            {
                if (!IsRunning)
                    Start();
                chkRun.Text = "En cours...";
            }
            else
            {
                Stop();
                chkRun.Text = "Démarrer";
            }
        }

        /**
         * 
         * 
         */
        #region Initialize_Objects
        private void Initialize_Objects(bool resetAll = false)
        {

            if (ImageProcesses != null)
                foreach (var handler in ImageProcesses)
                    if (resetAll)
                        handler.Dispose();
                    else
                        handler.Stop();

            if (ImageProcesses != null && !resetAll)
            {
                foreach (var handler in ImageProcesses)
                    handler.Stop();

                //Restaure delegates
                ImageProcess prevHandler = null;
                foreach (var handler in ImageProcesses)
                {
                    if (prevHandler != null)
                        handler.OnImageChanged += prevHandler.ImageChanged;
                    else
                        handler.OnImageChanged += this.ImageChanged;

                    prevHandler = handler;
                }

                return;
            }

            if (Performance == null || resetAll)
            {
                Performance = new("FWebCam", FLogger.Current.Logger);
            }

            if (ImageProcesses == null)
                ImageProcesses = new();
            else
                ImageProcesses.Clear();

            //Render
            if (Render == null || resetAll)
            {
                Render = new Render(
                    "Render"
                    , Performance.Sub("Render", chkRenderLogger.Checked, KnownColor.Yellow)
                    , this
                    );
                Render.Performance.IsColored = chkLogColored.Checked;
            }
            else
            {
                //ImageProcess.Stop() kills OnImageChanged register
                Render.OnImageChanged += this.ImageChanged;
            }
            //Add process
            ImageProcesses.Add(Render);

            ImageProcess imgProc;

            //MovingRegions
            imgProc = new EmguMoving(
                        "EmguMoving"

                , Performance.Sub("EmguMoving", chkRenderLogger.Checked, KnownColor.AliceBlue)
                        , this
                        , ImageProcesses.Last()
                    );

            ////MovingRegions
            //imgProc = new MovingRegions(
            //            "MovingRegions"

            //    , Performance.Sub("MovingRegions", chkRenderLogger.Checked, KnownColor.GreenYellow)
            //            , this
            //            , ImageProcesses.Last()
            //        );
            //Add process
            ImageProcesses.Add(imgProc);

            //WebCam
            if (WebCam == null || resetAll)
            {
                WebCam = new EDVideoCapture(
                "WebCam"
                , Performance.Sub("WebCam", chkVideoCaptureLogger.Checked, FLogger.Current.DefaultLoggerColor.ToKnownColor())
                , this
                , ImageProcesses.Last()
                );
                WebCam.Performance.IsColored = chkLogColored.Checked;
                WebCam.IsRunningChanged += WebCam_IsRunningChanged;
            }
            else
            {
                //ImageProcess.Stop() kills OnImageChanged register
                WebCam.OnImageChanged += ImageProcesses.Last().ImageChanged;
            }
            //WebCam.ImageSizeMax
            if (cboCaptureSize.Text == "")
            {
                if (!WebCam.ImageSizeMax.IsEmpty)
                    cboCaptureSize.Text = Core.Parser.SizeToPretty(WebCam.ImageSizeMax);
            }
            else
                WebCam.ImageSizeMax = Core.Parser.SizeFromPretty(cboCaptureSize.Text);
            //Add process
            ImageProcesses.Add(WebCam);
        }
        #endregion

        #region Process
        private void WebCam_IsRunningChanged(ImageProcess sender, bool isRunning)
        {
            if (isRunning)
            {
                chkRun.Checked = true;
            }
            else
                Stop();
        }

        /**
         * 
         * 
         */
        public void Start()
        {
            if (IsRunning)
            {
                if (ProcessState == System.Threading.ThreadState.Suspended)
                {
                    ProcessState = ThreadState.Running;
                }
                return;
            }

            ProcessState = ThreadState.Unstarted;

            Initialize_Objects(true);

            FLogger.Current.Start();

            foreach (var item in ImageProcesses)
            {
                if (item == WebCam)
                {
                    WebCam.CameraIndex = cboCameras.SelectedIndex;
                }
                item.Start();
            }

            chkRun.Checked = true;
        }

        /**
         * 
         * 
         */
        public void Stop()
        {
            if (chkRun.Checked)
                chkRun.Text = "Arrêt en cours...";

            if (!IsRunning)
                return;

            ProcessState = ThreadState.StopRequested;

            if (WebCam == null)
                return;

            foreach (var item in ImageProcesses.Reverse<ImageProcess>())
            {
                item.Stop();
            }

            FLogger.Current.Stop();

            chkRun.Checked = false;

            ProcessState = ThreadState.Stopped;
        }

        public void Resume()
        {
            if (IsRunning)
                foreach (var item in ImageProcesses)
                    item.Resume();
        }

        public void Pause()
        {
            if (IsRunning)
                foreach (var item in ImageProcesses)
                    item.Pause();
        }
        #endregion
        /**
         * Image
         * */
        public void ImageChanged(IImageProvider sender)
        {
            if (this.Disposing || this.IsDisposed)
                return;

            if (sender is Render)
                RefreshImage((Render)sender);
            FLogger.Current.RefreshProgress((ImageProcess)sender);

            FLogger.Current.ProgressMessage = $"Webcam [{WebCam.Performance.Counter}]";
        }
        private void RefreshImage(Render sender)
        {
            if (!sender.IsRunning)
                return;
            //Render.Performance.Step("RefreshImage call stack :\n" + Environment.StackTrace);
            Render.Performance.Resume("RefreshImage", true);
            if (sender.Image != null)
            {
                try
                {
                    picRender.Image = (sender as Render).Image;
                }
                catch (Exception ex)
                {
                    Render.Performance.Step(ex.ToString());
                }
            }
            Render.Performance.Pause();
        }


        private void chkRenderLogger_CheckedChanged(object sender, EventArgs e)
        {
            if (Render != null)
                Render.Performance.Enabled = chkRenderLogger.Checked;
        }
        private void chkVideoCaptureLogger_CheckedChanged(object sender, EventArgs e)
        {
            if (WebCam != null)
                WebCam.Performance.Enabled = chkVideoCaptureLogger.Checked;
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
                WebCam.ImageSizeMax = Size.Empty;
            else
                WebCam.ImageSizeMax = Core.Parser.SizeFromPretty(cboCaptureSize.Text);
        }
    }
}

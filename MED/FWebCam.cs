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
    public partial class FWebCam : Form, IImageConsumer
    {
        public FWebCam()
        {
            InitializeComponent();

            ImageProcesses = new();
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
                    Dock =DockStyle.Fill;
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
        WebCam WebCam;
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
            foreach (var cam in WebCam.AvailableCameras())
                cboCameras.Items.Add(cam);
            if (cboCameras.Items.Count > 0)
                cboCameras.SelectedIndex = 0;
        }

        Performance Performance;


        public bool IsRunning { get; private set; }

        private void chkRun_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRun.Checked)
            {
                Run();
                IsRunning = true;
                chkRun.Text = "En cours...";
            }
            else
            {
                Stop();
                IsRunning = false;
                chkRun.Text = "Démarrer";
            }
        }

        /**
         * 
         * 
         */
        private void Initialize_Objects()
        {
            Performance = new("FWebCam", FLogger.Current.Logger);

            ImageProcesses.Clear();

            //Render
            Render = new Render(
                "Render"
                , Performance.Sub("Render", chkRenderLogger.Checked, KnownColor.Yellow)
                , this
                );
            Render.Performance.IsColored = chkLogColored.Checked;
            ImageProcesses.Add(Render);

            //MovingRegions
            ImageProcess imgProc = new MED.Imaging.MovingRegions(
                "MovingRegions"
                , Performance.Sub("MovingRegions", chkRenderLogger.Checked, KnownColor.GreenYellow)
                , this
                , ImageProcesses.Last()
            );
            ImageProcesses.Add(imgProc);

            //WebCam
            WebCam = new WebCam(
                "WebCam"
                , Performance.Sub("WebCam", chkVideoCaptureLogger.Checked, FLogger.Current.DefaultLoggerColor.ToKnownColor())
                , this
                , ImageProcesses.Last()
                );
            WebCam.Performance.IsColored = chkLogColored.Checked;
            if (cboCaptureSize.Text == "")
            {
                if (!WebCam.ImageSizeMax.IsEmpty)
                    cboCaptureSize.Text = Core.Parser.SizeToPretty(WebCam.ImageSizeMax);
            }
            else
                WebCam.ImageSizeMax = Core.Parser.SizeFromPretty(cboCaptureSize.Text);
            ImageProcesses.Add(WebCam);
        }

        /**
         * 
         * 
         */
        private void Run()
        {
            Initialize_Objects();

            FLogger.Current.Run();

            foreach (var item in ImageProcesses)
            {
                if (item == WebCam)
                {
                    WebCam.CameraIndex = cboCameras.SelectedIndex;
                    WebCam.Run();
                }
                else
                {
                    item.Run();
                }
            }
        }

        /**
         * 
         * 
         */
        private void Stop()
        {
            if (chkRun.Checked)
                chkRun.Text = "Arrêt en cours...";
            IsRunning = false;
            if (WebCam == null)
                return;

            foreach (var item in ImageProcesses.Reverse<ImageProcess>())
            {
                item.Stop();
            }

            FLogger.Current.Stop();

            chkRun.Checked = false;
        }

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

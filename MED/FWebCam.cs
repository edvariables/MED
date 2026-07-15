using DirectShowLib;
using Emgu.CV;
using MED.Imaging;
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

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Init_AvailableCameras();

            Initialize_Logger();

            Initialize_Objects();

            LoadSettings();
        }


        private CheckBox chkLogColored;
        private CheckBox chkVideoCaptureLogger;
        private CheckBox chkRenderLogger;
        private void Initialize_Logger()
        {
            chkLogColored = (CheckBox)FLogger.Current.Controls["chkLogColored"];
            chkVideoCaptureLogger = (CheckBox)FLogger.Current.Controls["chkVideoCaptureLogger"];
            chkRenderLogger = (CheckBox)FLogger.Current.Controls["chkRenderLogger"];

            chkVideoCaptureLogger.CheckedChanged += chkVideoCaptureLogger_CheckedChanged;
            chkRenderLogger.CheckedChanged += chkRenderLogger_CheckedChanged;
            chkLogColored.CheckedChanged += chkLogColoredNot_CheckedChanged;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        List<ImageProcess> ImageProcesses = new();
        WebCam WebCam;
        Render Render;

        private void LoadSettings()
        {
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
            Performance = new("WebCam", FLogger.Current.ProgressMessage);

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
                , Performance.Sub("WebCam", chkVideoCaptureLogger.Checked, FLogger.Current.LoggerForeColor.ToKnownColor())
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
                    int nCamera = cboCameras.SelectedIndex;
                    WebCam.Run(nCamera);
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

        //private void InvokeRefreshRender()
        //{
        //    PerfImageRender.Step("InvokeRefresh", false);
        //    if (this.Disposing || this.IsDisposed)
        //        return;
        //    try
        //    {
        //        this.Invoke(RefreshRender);
        //    }
        //    catch { }
        //}
        public void ImageChanged(IImageProvider sender)
        {
            if (this.Disposing || this.IsDisposed)
                return;

            if (sender is Render)
                RefreshImage((Render)sender);
            FLogger.Current.RefreshProgress((ImageProcess)sender);

            this.Text = $"Webcam [{WebCam.Performance.Counter}]";
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
            Render.Performance.IsColored = chkLogColored.Checked;
            WebCam.Performance.IsColored = chkLogColored.Checked;
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

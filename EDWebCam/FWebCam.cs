using DirectShowLib;
using Emgu.CV;
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

            Initialize_Objects();

            LoadSettings();
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
            chkRenderLogger.Checked = Render.LoggerEnabled;
            chkVideoCaptureLogger.Checked = WebCam.LoggerEnabled;
        }
        private void SaveSettings()
        {
            Render.LoggerEnabled = chkRenderLogger.Checked;
            Render.SaveSettings();
            WebCam.LoggerEnabled = chkVideoCaptureLogger.Checked;
            WebCam.SaveSettings();
            Core.Settings.Save();
        }
        private void Init_AvailableCameras()
        {
            cboCameras.Items.Clear();
            foreach (var cam in WebCam.AvailableCameras())
                cboCameras.Items.Add(cam);
            if (cboCameras.Items.Count > 0)
                cboCameras.SelectedIndex = 0;
        }

        StringBuilder ProgressMessage = new();


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
            ImageProcesses.Clear();

            Render = new Render("Render", ProgressMessage, this);
            Render.LoggerEnabled = chkRenderLogger.Checked;
            Render.PerfColor = KnownColor.Yellow;
            Render.Performance.LoggerColored = chkLogColored.Checked;
            ImageProcesses.Add(Render);

            ImageProcess imgProc = new MED.Imaging.MovingRegions();


            WebCam = new WebCam("WebCam", ProgressMessage, this, Render);
            WebCam.LoggerEnabled = chkVideoCaptureLogger.Checked;
            WebCam.PerfColor = KnownColor.White;
            WebCam.Performance.LoggerColored = chkLogColored.Checked;
            ImageProcesses.Add(WebCam);
        }

        /**
         * 
         * 
         */
        private void Run()
        {
            Initialize_Objects();
            Render.Run();

            int nCamera = cboCameras.SelectedIndex;
            WebCam.Run(nCamera);

            RTGBAppendPerf?.Start();
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

            WebCam.Stop();
            Render.Stop();

            RTGBAppendPerf?.Stop();
            RTGBAppendRegex = null;
            RTGBAppendPerf = null;

            RefreshProgress(null);
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
            RefreshProgress((ImageProcess)sender);
        }
        private void RefreshImage(Render sender)
        {
            if (sender.Image == null || !sender.IsRunning)
                return;
            try
            {
                Render.Performance.Step("RefreshImage");
                picRender.Image = (sender as Render).Image;
            }
            catch (Exception ex)
            {
                rtbLog.AppendText("\n" + ex.ToString());
            }
        }

        private void RefreshProgress(ImageProcess sender)
        {
            if (ProgressMessage == null)
                return;
            if (ProgressMessage.Length > 0)
            {
                if (rtbLog.TextLength > 1024 * 1024)
                {
                    rtbLog.Select(0, rtbLog.TextLength / 2);
                    rtbLog.SelectedText = "";
                }
                rtbLog.SelectionStart = int.MaxValue;
                if (ProgressMessage.Length > 0)
                    //if (ProgressMessage[0] == '\b')
                    //{
                    RTGBAppend(rtbLog, ProgressMessage);
                //}
                //else
                //{
                //    rtbLog.AppendText(ProgressMessage.ToString());
                //    ProgressMessage.Clear();
                //}
                rtbLog.SelectionStart = int.MaxValue;
                rtbLog.ScrollToCaret();
            }
            this.Text = $"Webcam [{WebCam.Performance.Counter}]";
        }

        Regex RTGBAppendRegex = null;
        Performance RTGBAppendPerf;
        private void RTGBAppend(RichTextBox rtb, StringBuilder strB)
        {
            var str = strB.ToString();
            strB.Clear();
            if (str.Contains('\b'))
            {
                if (RTGBAppendRegex == null)
                {
                    RTGBAppendPerf = new Performance("RTGBAppend", strB, chkLogColored.Checked, KnownColor.MediumPurple);
                    var delimter = Regex.Escape("\b");
                    var pattern = $"{delimter}{Regex.Escape("{")}(?<property>(\\w|\\d)+){Regex.Escape(":")}(?<value>[^{delimter}]*){Regex.Escape("}")}=(?<log>[^{delimter}]*){delimter}(?<crlf>[^{delimter}]*)";
                    RTGBAppendRegex = new(pattern);
                    RTGBAppendPerf.LoggerColored = true;
                    RTGBAppendPerf.Start();
                }

                RTGBAppendPerf.Resume("RTGBAppendRegex.Matches", true);
                var matches = RTGBAppendRegex.Matches(str);
                foreach (var match in matches)
                {
                    var startSel = rtb.Text.Length;
                    rtb.SelectionStart = startSel;
                    string value = ((Match)match).Groups["value"].Value;
                    var prop = ((Match)match).Groups["property"].Value.ToLower();
                    switch (prop)
                    {
                        case "color":
                            rtb.SelectionColor = Color.FromKnownColor((KnownColor)Enum.Parse(typeof(KnownColor), value));
                            break;
                        default:
                            Console.WriteLine($"RTGBAppend : unknown property {prop}");
                            break;
                    }
                    rtb.AppendText(((Match)match).Groups["log"].Value);
                    rtb.SelectionStart = int.MaxValue;
                    rtb.SelectionColor = rtb.ForeColor;
                    rtb.AppendText(((Match)match).Groups["crlf"].Value);
                    //rtb.Select(startSel, rtb.Text.Length);
                }
                RTGBAppendPerf.Pause();
                if (matches.Count > 0)
                    return;
            }

            rtbLog.AppendText(str);

        }

        private void chkRenderLogger_CheckedChanged(object sender, EventArgs e)
        {
            if (Render != null)
                Render.LoggerEnabled = chkRenderLogger.Checked;
        }
        private void chkVideoCaptureLogger_CheckedChanged(object sender, EventArgs e)
        {
            if (WebCam != null)
                WebCam.LoggerEnabled = chkVideoCaptureLogger.Checked;
        }
        private void chkLogColoredNot_CheckedChanged(object sender, EventArgs e)
        {
            if (Render == null)
                return;
            Render.Performance.LoggerColored = chkLogColored.Checked;
            WebCam.Performance.LoggerColored = chkLogColored.Checked;
            if (RTGBAppendPerf != null)
                RTGBAppendPerf.LoggerColored = chkLogColored.Checked;
        }

        private void cmdSaveSettings_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }


        private void SplitterLog_SplitterMoving(object sender, SplitterEventArgs e)
        {
            var delta = e.SplitY - e.Y;
            this.Text = $"delta = {delta} = {e.SplitY} - {e.Y}, rtbLog.Height = {rtbLog.Height}";
            this.Text = $"delta = {delta} = {e.SplitY} - {e.Y}, rtbLog.Top = {rtbLog.Top}, rtbLog.Height = {rtbLog.Height} => {rtbLog.Bottom - e.SplitY + delta}";
            //this.Text = $"delta = {delta} = {e.SplitY} - {e.Y}, tableLayoutPan.Top = {tableLayoutPan.Top}, tableLayoutPan.Bottom = {tableLayoutPan.Bottom}, tableLayoutPan.Height = {tableLayoutPan.Height}";
            rtbLog.Size = new Size(rtbLog.Width, rtbLog.Bottom - e.SplitY + delta);
            tableLayoutPan.Size = new Size(tableLayoutPan.Width, e.Y-5);
        }

        private void SplitterLog_SplitterMoved(object sender, SplitterEventArgs e)
        {
            var delta = e.SplitY - e.Y;
            this.Text = $"delta = {delta} = {e.SplitY} - {e.Y}, rtbLog.Top = {rtbLog.Top}, rtbLog.Height = {rtbLog.Height} => {rtbLog.Bottom - e.SplitY + delta}";

        }

        private void cmdNewForm_Click(object sender, EventArgs e)
        {
            (new FWebCam()).Show();
        }

    }
}

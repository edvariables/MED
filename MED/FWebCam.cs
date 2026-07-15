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
            chkRenderLogger.Checked = Render.Performance.Enabled;
            chkVideoCaptureLogger.Checked = WebCam.Performance.Enabled;

            chkClearLogOnRun.Checked = (bool)Core.Settings.GetValue("ClearLogOnRun", this.Name, chkClearLogOnRun.Checked);

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

            Core.Settings.SetValue("ClearLogOnRun", this.Name, chkClearLogOnRun.Checked);
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
            Performance = new("WebCam", ProgressMessage);

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
                , Performance.Sub("WebCam", chkVideoCaptureLogger.Checked, rtbLog.ForeColor.ToKnownColor())
                , this
                , ImageProcesses.Last()
                );
            WebCam.Performance.IsColored = chkLogColored.Checked;
            if (cboCaptureSize.Text == "")
            {
                if ( ! WebCam.ImageSizeMax.IsEmpty)
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

            if (chkClearLogOnRun.Checked)
                rtbLog.Clear();

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

            foreach (var item in ImageProcesses.Reverse<ImageProcess>())
            {
                item.Stop();
            }

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
                    rtbLog.AppendText("\n" + ex.ToString());
                }
            }
            Render.Performance.Pause();
        }

        private void RefreshProgress(ImageProcess sender)
        {
            if (ProgressMessage == null)
                return;
            if (ProgressMessage.Length > 0)
            {
                rtbLog.SuspendLayout();

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

                rtbLog.ResumeLayout();
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
                    RTGBAppendPerf.IsColored = true;
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
            if (RTGBAppendPerf != null)
                RTGBAppendPerf.IsColored = chkLogColored.Checked;
        }

        private void ChkClearLogOnRun_CheckStateChanged(object sender, EventArgs e)
        {
            if (chkClearLogOnRun.CheckState != CheckState.Unchecked)
                rtbLog.Clear();
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
            tableLayoutPan.Size = new Size(tableLayoutPan.Width, e.Y - 5);
        }

        private void SplitterLog_SplitterMoved(object sender, SplitterEventArgs e)
        {
            var delta = e.SplitY - e.Y;
            this.Text = $"delta = {delta} = {e.SplitY} - {e.Y}, rtbLog.Top = {rtbLog.Top}, rtbLog.Height = {rtbLog.Height} => {rtbLog.Bottom - e.SplitY + delta}";

        }

        private void cboCaptureSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCaptureSize.Text == "")
                WebCam.ImageSizeMax = Size.Empty;
            else
                WebCam.ImageSizeMax = Core.Parser.SizeFromPretty(cboCaptureSize.Text);
        }

        private long _chkClearLogOnRun_CheckedChanged_ticks = 0L;
        private void chkClearLogOnRun_CheckedChanged(object sender, EventArgs e)
        {
            if (!((Control)sender).ContainsFocus)
                return;
            var now = DateTime.Now.Ticks;
            if ((now -_chkClearLogOnRun_CheckedChanged_ticks)/TimeSpan.TicksPerSecond < 1)
            {
                //Double-click < 1sec
                rtbLog.Clear();
            }
            _chkClearLogOnRun_CheckedChanged_ticks = now;

        }
    }
}

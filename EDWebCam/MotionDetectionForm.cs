using Emgu.CV;
using Emgu.CV.Structure;
using libMotionDetection;
using libVideoCapture;
using Serilog;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MotionDetectionWinFormsApp
{
    public partial class MotionDetectionForm : Form
    {
        private static readonly ILogger logger = Log.Logger.ForContext (typeof (MotionDetectionForm));

        private MotionDetectionWithDenseOpticalFlow motionDetectionWithDenseOpticalFlow;
        private MotionDetectionWithMotionHistory motionDetectionWithMotionHistory;
        private MotionDetectionWithSparseOpticalFlow motionDetectionWithSparseOpticalFlow;
        private MotionDetectionWithFrameDifferencing motionDetectionWithFrameDifferencing;
        private MotionDetectionWithBackgroundSubtraction motionDetectionWithBackgroundSubtraction;
        private VideoCaptureDevices videoCaptureDevices;
        private VideoCapture _capture = null;
        private string videoFilename = null;
        private string rtspURI = null;

        public MotionDetectionForm ()
        {
            InitializeComponent ();

            videoCaptureDevices = new VideoCaptureDevices ();
            logger.Information ($"Capture Devices: {string.Join ("\n", videoCaptureDevices.VideoInputDevices)}");

            // Populate combo box with capture devices
            foreach (VideoCaptureDevices.DsVideoInputDevice capDevice in videoCaptureDevices.VideoInputDevices) {
                comboBoxCaptureDevice.Items.Add ($"{capDevice.VideoInputDevice.Name}");
            }
            // Initialize the combo box to first value
            comboBoxCaptureDevice.SelectedIndex = 0;
            InitializeCapture (comboBoxCaptureDevice.SelectedIndex);

            // Populate algorithm combo box and set default to Dense Optical Flow
            comboBoxAlgorithm.SelectedIndex = 0;

            // Set settings controls enablement and defaults
            bool isMotionHistory = comboBoxAlgorithm.SelectedIndex == 1;
            textBoxMotionThreshold.Enabled = isMotionHistory;
            checkBoxCalculateMotionInfo.Enabled = isMotionHistory;
            textBoxMotionPixelCountThresholdPerCentArea.Enabled = isMotionHistory;

            // Initialize settings values from defaults
            textBoxMotionThreshold.Text = motionDetectionWithMotionHistory.Setting.MotionThreshold.ToString ();
            checkBoxCalculateMotionInfo.Checked = motionDetectionWithMotionHistory.Setting.CalculateMotionInfo;
            textBoxMotionPixelCountThresholdPerCentArea.Text = motionDetectionWithMotionHistory.Setting.MotionPixelCountThresholdPerCentArea.ToString ();
        }

        /// <summary>
        /// Initialize capture device and start capturing frames
        /// </summary>
        private void InitializeCapture (int deviceIndex = 0)
        {
            if (_capture != null) {
                _capture.Stop ();
                _capture.ImageGrabbed -= ProcessFrame;
                _capture.Dispose ();
                _capture = null;
            }

            // Clear and dispose existing ImageBox images to avoid leaving stale frames in UI
            SafeSetImageBoxImage (capturedImageBox, null);
            SafeSetImageBoxImage (motionImageBox, null);
            SafeSetImageBoxImage (forgroundImageBox, null);

            // Try to create the capture
            if (_capture == null) {
                try {
                    if (videoFilename != null) {
                        _capture = new VideoCapture (videoFilename);
                        videoFilename = null;
                    } else if (rtspURI != null) {
                        _capture = new VideoCapture (rtspURI);
                        rtspURI = null;
                    } else {
                        _capture = new VideoCapture (deviceIndex);
                    }
                } catch (NullReferenceException ex) {
                    MessageBox.Show (ex.Message);
                    logger.Error ($"{ex.Message}");
                }
            }

            // if camera capture has been successfully created
            if (_capture != null) {
                motionDetectionWithDenseOpticalFlow?.Dispose ();
                motionDetectionWithDenseOpticalFlow = new MotionDetectionWithDenseOpticalFlow ();

                motionDetectionWithMotionHistory?.Dispose ();
                motionDetectionWithMotionHistory = new MotionDetectionWithMotionHistory ();

                motionDetectionWithSparseOpticalFlow?.Dispose ();
                motionDetectionWithSparseOpticalFlow = new MotionDetectionWithSparseOpticalFlow ();

                motionDetectionWithFrameDifferencing?.Dispose ();
                motionDetectionWithFrameDifferencing = new MotionDetectionWithFrameDifferencing ();

                motionDetectionWithBackgroundSubtraction?.Dispose ();
                motionDetectionWithBackgroundSubtraction = new MotionDetectionWithBackgroundSubtraction ();

                _capture.ImageGrabbed += InvokeProcessFrame;
                _capture.Start ();
            }
        }

        private Mat currentFrame = new Mat ();
        private Mat previousFrame = new Mat ();

        private void SafeSetImageBoxImage (Emgu.CV.UI.ImageBox imageBox, Mat newImage)
        {
            var oldImage = imageBox.Image;
            imageBox.Image = newImage;
            if (oldImage != null && oldImage != newImage) {
                oldImage.Dispose ();
            }
        }

        private void InvokeProcessFrame (object sender, EventArgs e)
        {
            this.Invoke (ProcessFrame, sender, e);
        }

        private bool MatIsEmpty (Mat frame)
        {
            foreach (var b in frame.GetRawData ())
                if (b != 0)
                    return false;
            return true;
        }

        private void ProcessFrame (object sender, EventArgs e)
        {
            if (this.Disposing || this.IsDisposed)
                return;

            if (comboBoxAlgorithm.SelectedIndex <= 0) {
                // Dense Optical Flow
                if (_capture.Retrieve (currentFrame)) {
                    if (!currentFrame.Size.IsEmpty && !previousFrame.Size.IsEmpty &&
                        currentFrame.Size == previousFrame.Size) {

                        Mat flow = motionDetectionWithDenseOpticalFlow.CalculateDenseOpticalFlow (previousFrame, currentFrame);

                        SafeSetImageBoxImage (motionImageBox, motionDetectionWithDenseOpticalFlow.OpticalFlowVisualizationWithHSV (flow));
                        flow.Dispose ();
                        
                        SafeSetImageBoxImage (capturedImageBox, currentFrame.Clone ());

                        Mat frameDiff = currentFrame.Clone ();
                        CvInvoke.AbsDiff (previousFrame, currentFrame, frameDiff);
                        SafeSetImageBoxImage (forgroundImageBox, frameDiff);

                        Console.WriteLine ($"MatIsEmpty : {MatIsEmpty (frameDiff)}");
                    }
                    
                    var oldPrev = previousFrame;
                    previousFrame = currentFrame.Clone ();
                    oldPrev?.Dispose ();
                }
            } else if (comboBoxAlgorithm.SelectedIndex == 1) {
                // Motion History
                Mat image = new Mat ();

                if (_capture.Retrieve (image)) {
                    motionDetectionWithMotionHistory.GetFrameMotionComponents (image);

                    // If the check box for motion info is checked, then calculate the motion image
                    if (checkBoxCalculateMotionInfo.Checked) {
                        Mat motionImage = motionDetectionWithMotionHistory.GetMotionImage ();
                        SafeSetImageBoxImage (motionImageBox, motionImage);
                    } else {
                        SafeSetImageBoxImage (motionImageBox, null);
                    }

                    motionDetectionWithMotionHistory.MotionDetectionDrawGraphics (image);

                    if (this.Disposing || this.IsDisposed) {
                        image.Dispose ();
                        return;
                    }

                    SafeSetImageBoxImage (capturedImageBox, image);
                    SafeSetImageBoxImage (forgroundImageBox, motionDetectionWithMotionHistory.MotionForgroundMask.Clone ());

                    // Display the amount of motions found on the current image
                    var components = motionDetectionWithMotionHistory.MotionComponents;
                    UpdateText ($"Total Motions found: {components.Length}");
                    logger.Information ($"Total Motions found: {components.Length}");

                    int idx = 0;
                    foreach (MotionDetectionWithMotionHistory.MotionComponent comp in components) {
                        UpdateText ($"Motion Component {idx}: {comp}");
                        logger.Information ($"Motion Component {idx}: {comp}");
                        idx++;
                    }
                } else {
                    image.Dispose ();
                }
            } else if (comboBoxAlgorithm.SelectedIndex == 2) {
                // Sparse Optical Flow (Lucas-Kanade)
                Mat image = new Mat ();

                if (_capture.Retrieve (image)) {
                    motionDetectionWithSparseOpticalFlow.ProcessFrame (image);

                    if (this.Disposing || this.IsDisposed) {
                        image.Dispose ();
                        return;
                    }

                    // 1. Main visualizer: Frame with overlay
                    Mat displayImage = image.Clone ();
                    motionDetectionWithSparseOpticalFlow.DrawMotionVectors (displayImage);
                    SafeSetImageBoxImage (capturedImageBox, displayImage);

                    // 2. Motion visualizer: Isolated vectors on black background
                    Mat motionMat = new Mat (image.Size, image.Depth, image.NumberOfChannels);
                    motionMat.SetTo (new MCvScalar (0, 0, 0));
                    motionDetectionWithSparseOpticalFlow.DrawMotionVectors (motionMat);
                    SafeSetImageBoxImage (motionImageBox, motionMat);

                    // 3. Foreground visualizer: Temporal frame differencing
                    if (!previousFrame.Size.IsEmpty && previousFrame.Size == image.Size) {
                        Mat frameDiff = image.Clone ();
                        CvInvoke.AbsDiff (previousFrame, image, frameDiff);
                        SafeSetImageBoxImage (forgroundImageBox, frameDiff);
                    } else {
                        SafeSetImageBoxImage (forgroundImageBox, null);
                    }

                    // Update previous frame for differencing
                    var oldPrev = previousFrame;
                    previousFrame = image.Clone ();
                    oldPrev?.Dispose ();

                    image.Dispose ();

                    var vectors = motionDetectionWithSparseOpticalFlow.MotionVectors;
                    UpdateText ($"Active Tracked Points: {vectors.Count}");
                    logger.Information ($"Active Tracked Points: {vectors.Count}");
                } else {
                    image.Dispose ();
                }
            } else if (comboBoxAlgorithm.SelectedIndex == 3) {
                // Temporal Frame Differencing (AbsDiff)
                Mat image = new Mat ();

                if (_capture.Retrieve (image)) {
                    motionDetectionWithFrameDifferencing.ProcessFrame (image);

                    if (this.Disposing || this.IsDisposed) {
                        image.Dispose ();
                        return;
                    }

                    // 1. Captured visualizer: Raw frame with bounding boxes overlaid
                    Mat displayImage = image.Clone ();
                    motionDetectionWithFrameDifferencing.DrawMotionGraphics (displayImage);
                    SafeSetImageBoxImage (capturedImageBox, displayImage);

                    // 2. Motion visualizer: Grayscale raw diff
                    SafeSetImageBoxImage (motionImageBox, motionDetectionWithFrameDifferencing.MotionRawDiff.Clone ());

                    // 3. Foreground visualizer: Binary thresholded mask
                    SafeSetImageBoxImage (forgroundImageBox, motionDetectionWithFrameDifferencing.MotionForgroundMask.Clone ());

                    image.Dispose ();

                    var components = motionDetectionWithFrameDifferencing.MotionComponents;
                    UpdateText ($"Total Motions found: {components.Length}");
                    logger.Information ($"Total Motions found: {components.Length}");

                    int idx = 0;
                    foreach (Rectangle comp in components) {
                        UpdateText ($"Motion Box {idx}: {comp}");
                        logger.Information ($"Motion Box {idx}: {comp}");
                        idx++;
                    }
                } else {
                    image.Dispose ();
                }
            } else {
                // Background Subtraction (MOG2 / KNN / CNT / GMG)
                int index = comboBoxAlgorithm.SelectedIndex;
                MotionDetectionWithBackgroundSubtraction.SubtractorType subtractorType =
                    index == 4 ? MotionDetectionWithBackgroundSubtraction.SubtractorType.MOG2 :
                    index == 5 ? MotionDetectionWithBackgroundSubtraction.SubtractorType.KNN :
                    index == 6 ? MotionDetectionWithBackgroundSubtraction.SubtractorType.CNT :
                                 MotionDetectionWithBackgroundSubtraction.SubtractorType.GMG;

                motionDetectionWithBackgroundSubtraction.ActiveSubtractor = subtractorType;

                Mat image = new Mat ();

                if (_capture.Retrieve (image)) {
                    motionDetectionWithBackgroundSubtraction.ProcessFrame (image);

                    if (this.Disposing || this.IsDisposed) {
                        image.Dispose ();
                        return;
                    }

                    // 1. Captured visualizer: Raw frame with bounding boxes overlaid
                    Mat displayImage = image.Clone ();
                    motionDetectionWithBackgroundSubtraction.DrawMotionGraphics (displayImage);
                    SafeSetImageBoxImage (capturedImageBox, displayImage);

                    // 2. Motion visualizer: Raw camera frame
                    SafeSetImageBoxImage (motionImageBox, image.Clone ());

                    // 3. Foreground visualizer: Binary mask
                    SafeSetImageBoxImage (forgroundImageBox, motionDetectionWithBackgroundSubtraction.MotionForgroundMask.Clone ());

                    image.Dispose ();

                    var components = motionDetectionWithBackgroundSubtraction.MotionComponents;
                    UpdateText ($"Total Motions found: {components.Length}");
                    logger.Information ($"Total Motions found: {components.Length}");

                    int idx = 0;
                    foreach (Rectangle comp in components) {
                        UpdateText ($"Motion Box {idx}: {comp}");
                        logger.Information ($"Motion Box {idx}: {comp}");
                        idx++;
                    }
                } else {
                    image.Dispose ();
                }
            }
        }

        private void UpdateText (string text)
        {
            if (!IsDisposed && !Disposing && InvokeRequired) {
                Invoke ((Action<string>) UpdateText, text);
            } else {
                label3.Text = text;
            }
        }

        private void MotionDetectionForm_FormClosed (object sender, FormClosedEventArgs e)
        {
            _capture.Stop ();
            _capture.ImageGrabbed -= ProcessFrame;
            _capture.Dispose ();
            _capture = null;

            if (motionDetectionWithDenseOpticalFlow != null) {
                motionDetectionWithDenseOpticalFlow.Dispose ();
                motionDetectionWithDenseOpticalFlow = null;
            }

            if (motionDetectionWithMotionHistory != null) {
                motionDetectionWithMotionHistory.Dispose ();
                motionDetectionWithMotionHistory = null;
            }

            if (motionDetectionWithSparseOpticalFlow != null) {
                motionDetectionWithSparseOpticalFlow.Dispose ();
                motionDetectionWithSparseOpticalFlow = null;
            }

            if (motionDetectionWithFrameDifferencing != null) {
                motionDetectionWithFrameDifferencing.Dispose ();
                motionDetectionWithFrameDifferencing = null;
            }

            if (motionDetectionWithBackgroundSubtraction != null) {
                motionDetectionWithBackgroundSubtraction.Dispose ();
                motionDetectionWithBackgroundSubtraction = null;
            }

            if (currentFrame != null) {
                currentFrame.Dispose ();
                currentFrame = null;
            }

            if (previousFrame != null) {
                previousFrame.Dispose ();
                previousFrame = null;
            }

            capturedImageBox.Image?.Dispose ();
            motionImageBox.Image?.Dispose ();
            forgroundImageBox.Image?.Dispose ();
        }

        private void comboBoxCaptureDevice_SelectedIndexChanged (object sender, EventArgs e)
        {
            logger.Debug ($"Selected Capture device: {comboBoxCaptureDevice.Text}");
            string selectedCaptureDeviceName = comboBoxCaptureDevice.Text;
            int i = 0;

            foreach (VideoCaptureDevices.DsVideoInputDevice capDevice in videoCaptureDevices.VideoInputDevices) {
                if (capDevice.VideoInputDevice.Name.Equals (selectedCaptureDeviceName)) {
                    comboBoxCaptureDevice.SelectedIndex = i;

                    logger.Debug ($"Selected Capture device index: {comboBoxCaptureDevice.SelectedIndex}");
                    break;
                }
                i++;
            }

            // If there is a capture, stop it
            if (_capture != null) {
                _capture.Stop ();
                _capture.ImageGrabbed -= ProcessFrame;
                _capture.Dispose ();
                _capture = null;
            }

            InitializeCapture (comboBoxCaptureDevice.SelectedIndex);
        }

        private void comboBoxAlgorithm_SelectedIndexChanged (object sender, EventArgs e)
        {
            int index = comboBoxAlgorithm.SelectedIndex;
            bool isMotionHistory = index == 1;
            bool isFrameDiff = index == 3;

            textBoxMotionThreshold.Enabled = isMotionHistory || isFrameDiff;
            checkBoxCalculateMotionInfo.Enabled = isMotionHistory;
            textBoxMotionPixelCountThresholdPerCentArea.Enabled = isMotionHistory || isFrameDiff;

            if (isMotionHistory && motionDetectionWithMotionHistory != null) {
                textBoxMotionThreshold.Text = motionDetectionWithMotionHistory.Setting.MotionThreshold.ToString ();
                textBoxMotionPixelCountThresholdPerCentArea.Text = motionDetectionWithMotionHistory.Setting.MotionPixelCountThresholdPerCentArea.ToString ();
            } else if (isFrameDiff && motionDetectionWithFrameDifferencing != null) {
                textBoxMotionThreshold.Text = motionDetectionWithFrameDifferencing.Setting.Threshold.ToString ();
                textBoxMotionPixelCountThresholdPerCentArea.Text = motionDetectionWithFrameDifferencing.Setting.MinAreaPercent.ToString ();
            }

            SafeSetImageBoxImage (capturedImageBox, null);
            SafeSetImageBoxImage (motionImageBox, null);
            SafeSetImageBoxImage (forgroundImageBox, null);
            UpdateText ("");
        }

        private void textBoxMotionThreshold_TextChanged (object sender, EventArgs e)
        {
            try {
                int value = Int32.Parse (textBoxMotionThreshold.Text);
                if (comboBoxAlgorithm.SelectedIndex == 1 && motionDetectionWithMotionHistory != null) {
                    MotionDetectionWithMotionHistory.MotionSetting setting = motionDetectionWithMotionHistory.Setting;
                    setting.MotionThreshold = value;
                    motionDetectionWithMotionHistory.Setting = setting;
                } else if (comboBoxAlgorithm.SelectedIndex == 3 && motionDetectionWithFrameDifferencing != null) {
                    MotionDetectionWithFrameDifferencing.DifferenceSetting setting = motionDetectionWithFrameDifferencing.Setting;
                    setting.Threshold = value;
                    motionDetectionWithFrameDifferencing.Setting = setting;
                }
            } catch (FormatException ex) {
                logger.Error ($"Invalid motion threshold format: {ex.Message}");
            }
        }

        private void checkBoxCalculateMotionInfo_CheckedChanged (object sender, EventArgs e)
        {
            if (motionDetectionWithMotionHistory == null) return;
            MotionDetectionWithMotionHistory.MotionSetting setting = motionDetectionWithMotionHistory.Setting;
            setting.CalculateMotionInfo = checkBoxCalculateMotionInfo.Checked;
            motionDetectionWithMotionHistory.Setting = setting;
        }

        private void textBoxMotionPixelCountThresholdPerCentArea_TextChanged (object sender, EventArgs e)
        {
            try {
                double value = double.Parse (textBoxMotionPixelCountThresholdPerCentArea.Text);
                if (comboBoxAlgorithm.SelectedIndex == 1 && motionDetectionWithMotionHistory != null) {
                    MotionDetectionWithMotionHistory.MotionSetting setting = motionDetectionWithMotionHistory.Setting;
                    setting.MotionPixelCountThresholdPerCentArea = value;
                    motionDetectionWithMotionHistory.Setting = setting;
                } else if (comboBoxAlgorithm.SelectedIndex == 3 && motionDetectionWithFrameDifferencing != null) {
                    MotionDetectionWithFrameDifferencing.DifferenceSetting setting = motionDetectionWithFrameDifferencing.Setting;
                    setting.MinAreaPercent = value;
                    motionDetectionWithFrameDifferencing.Setting = setting;
                }
            } catch (FormatException ex) {
                logger.Error ($"Invalid motion pixel count threshold format: {ex.Message}");
            }
        }

        private Point MotionZoneStartPoint;
        private Point MotionZoneCurrentPoint;
        private Rectangle MotionZone = new Rectangle ();
        private bool doRectangle = false;

        private void capturedImageBox_MouseDown (object sender, MouseEventArgs e)
        {
            if (comboBoxAlgorithm.SelectedIndex != 1) return;

            if (e.Button == MouseButtons.Left) {
                MotionZoneStartPoint = e.Location;
                doRectangle = true;
            } else if (e.Button == MouseButtons.Right) {
                bool doRemoveAllMotionZones = true;

                Rectangle motionZoneToRemove = new Rectangle ();
                foreach (Rectangle motionZone in motionDetectionWithMotionHistory.MotionZones) {
                    if (motionZone.Contains (e.Location)) {
                        motionZoneToRemove = motionZone;
                        doRemoveAllMotionZones = false;
                        break;
                    }
                }

                if (!doRemoveAllMotionZones) {
                    motionDetectionWithMotionHistory.RemoveMotionZone (motionZoneToRemove);
                }

                if (doRemoveAllMotionZones) {
                    motionDetectionWithMotionHistory.ClearMotionZones ();
                }
            }
        }

        private void capturedImageBox_MouseMove (object sender, MouseEventArgs e)
        {
            if (comboBoxAlgorithm.SelectedIndex != 1) return;

            if (e.Button == MouseButtons.Left) {
                MotionZoneCurrentPoint = e.Location;
            }
        }

        private void capturedImageBox_MouseUp (object sender, MouseEventArgs e)
        {
            if (comboBoxAlgorithm.SelectedIndex != 1) return;

            if (e.Button == MouseButtons.Left) {
                if (doRectangle) {
                    Point motionZoneEndPoint = e.Location;

                    MotionZone.Location = new Point (
                        Math.Min (MotionZoneStartPoint.X, motionZoneEndPoint.X),
                        Math.Min (MotionZoneStartPoint.Y, motionZoneEndPoint.Y));
                    MotionZone.Size = new Size (
                        Math.Abs (MotionZoneStartPoint.X - motionZoneEndPoint.X),
                        Math.Abs (MotionZoneStartPoint.Y - motionZoneEndPoint.Y));

                    motionDetectionWithMotionHistory.AddMotionZone (MotionZone);

                    doRectangle = false;
                }
            }
        }

        private void capturedImageBox_Paint (object sender, PaintEventArgs e)
        {
            if (comboBoxAlgorithm.SelectedIndex != 1) return;

            if (doRectangle) {
                MotionZone.Location = new Point (
                    Math.Min (MotionZoneStartPoint.X, MotionZoneCurrentPoint.X),
                    Math.Min (MotionZoneStartPoint.Y, MotionZoneCurrentPoint.Y));
                MotionZone.Size = new Size (
                    Math.Abs (MotionZoneStartPoint.X - MotionZoneCurrentPoint.X),
                    Math.Abs (MotionZoneStartPoint.Y - MotionZoneCurrentPoint.Y));

                e.Graphics.DrawRectangle (Pens.Red, MotionZone);
            }
        }

        private void selectFileButton_Click (object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog () == DialogResult.OK) {
                logger.Debug ($"Selected file: {openFileDialog1.FileName}");

                if (_capture != null) {
                    _capture.Stop ();
                    _capture.ImageGrabbed -= ProcessFrame;
                    _capture.Dispose ();
                    _capture = null;
                }

                videoFilename = openFileDialog1.FileName;

                InitializeCapture (-1);
            }
        }

        private void rtspURITextBox_TextChanged (object sender, EventArgs e)
        {
            logger.Debug ($"RTSP URI: {rtspURITextBox.Text}");

            if (_capture != null) {
                _capture.Stop ();
                _capture.ImageGrabbed -= ProcessFrame;
                _capture.Dispose ();
                _capture = null;
            }

            rtspURI = rtspURITextBox.Text;

            InitializeCapture (-1);
        }
    }
}

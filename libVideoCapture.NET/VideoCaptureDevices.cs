using DirectShowLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace libVideoCapture
{
    /// <summary>
    /// A class for information for the video capture devices.
    /// Currently the name and the available resolutions are retrieved.
    /// </summary>
    public class VideoCaptureDevices
    {
        private static readonly ILogger logger = Log.Logger.ForContext (typeof (VideoCaptureDevices));

        /// <summary>
        /// A video input device struct which has the device
        /// and the available resolutions of the device
        /// </summary>
        public struct DsVideoInputDevice
        {
            public DsDevice VideoInputDevice { get; set; }

            public List<Size> AvailableResolutions { get; set; }

            public override string ToString () =>
                $"VideoInputDevice: {VideoInputDevice.Name} ({VideoInputDevice.DevicePath}), " +
                $"AvailableResolutions: {string.Join (", ", AvailableResolutions)}";
        }
        public DsVideoInputDevice[] VideoInputDevices { get; private set; } = { };

        public VideoCaptureDevices ()
        {
            GetAvailableVideoInputDevicesWithResolutions ();
        }

        /// <summary>
        /// Get available video input devices and their resolutions
        /// </summary>
        private void GetAvailableVideoInputDevicesWithResolutions ()
        {
            DsDevice[] videoInputDevices = DsDevice.GetDevicesOfCat (FilterCategory.VideoInputDevice);

            VideoInputDevices = new DsVideoInputDevice[videoInputDevices.Length];

            int i = 0;
            foreach (DsDevice videoInputDevice in videoInputDevices) {
                VideoInputDevices[i].VideoInputDevice = videoInputDevice;
                VideoInputDevices[i].AvailableResolutions = GetVideoCapabilities (videoInputDevice);
                i++;
            }
        }

        /// <summary>
        /// Get a list of available resolutions for a specific device.
        /// </summary>
        /// <param name="videoInputDevice">The device to get the capabilities</param>
        /// <returns>On success, the list of resolutions. On failure, an empty list</returns>
        private List<Size> GetVideoCapabilities (DsDevice videoInputDevice)
        {
            logger.Debug ($"videoInputDevice: {videoInputDevice.Name}, {videoInputDevice.DevicePath}");

            List<Size> availableResolutions = new List<Size> ();

            IFilterGraph2 filterGraph = null;
            IBaseFilter sourceFilter = null;
            IPin pinRaw = null;
            IEnumMediaTypes mediaTypeEnum = null;
            AMMediaType[] mediaTypes = new AMMediaType[1];

            try {
                filterGraph = new FilterGraph () as IFilterGraph2;
                if (filterGraph == null) {
                    throw new InvalidOperationException ("Failed to create FilterGraph.");
                }

                int hr = filterGraph.AddSourceFilterForMoniker (videoInputDevice.Mon, null, videoInputDevice.Name, out sourceFilter);
                DsError.ThrowExceptionForHR (hr);

                pinRaw = DsFindPin.ByDirection (sourceFilter, PinDirection.Output, 0);
                if (pinRaw == null) {
                    logger.Warning ("No output pin found on source filter.");
                    return availableResolutions;
                }

                hr = pinRaw.EnumMediaTypes (out mediaTypeEnum);
                DsError.ThrowExceptionForHR (hr);

                int max = 0;
                int bitCount = 0;
                VideoInfoHeader videoInfoHeader = new VideoInfoHeader ();

                // Check media types. Next returns 0 (S_OK) if it retrieves the requested number of items (1),
                // or 1 (S_FALSE) if it retrieves fewer.
                hr = mediaTypeEnum.Next (1, mediaTypes, IntPtr.Zero);
                while (hr == 0 && mediaTypes[0] != null) {
                    try {
                        if (mediaTypes[0].formatPtr != IntPtr.Zero) {
                            Marshal.PtrToStructure (mediaTypes[0].formatPtr, videoInfoHeader);
                            if (videoInfoHeader.BmiHeader.Size != 0 && videoInfoHeader.BmiHeader.BitCount != 0) {
                                if (videoInfoHeader.BmiHeader.BitCount > bitCount) {
                                    availableResolutions.Clear ();
                                    max = 0;
                                    bitCount = videoInfoHeader.BmiHeader.BitCount;
                                }
                                availableResolutions.Add (new Size (videoInfoHeader.BmiHeader.Width, videoInfoHeader.BmiHeader.Height));
                                if (videoInfoHeader.BmiHeader.Width > max || videoInfoHeader.BmiHeader.Height > max) {
                                    max = Math.Max (videoInfoHeader.BmiHeader.Width, videoInfoHeader.BmiHeader.Height);
                                }
                            }
                        }
                    } finally {
                        DsUtils.FreeAMMediaType (mediaTypes[0]);
                        mediaTypes[0] = null;
                    }

                    hr = mediaTypeEnum.Next (1, mediaTypes, IntPtr.Zero);
                }

                logger.Debug ($"Resolutions: {string.Join (", ", availableResolutions)}");

            } catch (Exception ex) {
                logger.Error ($"Error: {ex.Message}");
            } finally {
                if (mediaTypes[0] != null) {
                    DsUtils.FreeAMMediaType (mediaTypes[0]);
                    mediaTypes[0] = null;
                }
                if (mediaTypeEnum != null) {
                    Marshal.ReleaseComObject (mediaTypeEnum);
                }
                if (pinRaw != null) {
                    Marshal.ReleaseComObject (pinRaw);
                }
                if (sourceFilter != null) {
                    Marshal.ReleaseComObject (sourceFilter);
                }
                if (filterGraph != null) {
                    Marshal.ReleaseComObject (filterGraph);
                }
            }

            return availableResolutions;
        }
    }
}

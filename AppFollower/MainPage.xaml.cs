using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using AppFollower.Resources;
using TextureGraph;
using libvideo;
using FaceDetection;
using System.Windows.Resources;
using Windows.Phone.Media.Capture;
// NOTE: Need this for .AsInputStream()!
using System.IO;
using System.Threading.Tasks;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading;


namespace AppFollower
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region [Private members]
        private TextureGraphInterop texInterop;
        private Camera cam;
        private Detector detector;
        private ImageProcessing im;
        private StreamSocket s = null;
        private DataWriter output = null;
        private Task<bool> setupOK;
        private int[] mvs = new int[2];
        #endregion
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            StreamResourceInfo sri = Application.GetResourceStream(new Uri("Models\\face.xml", UriKind.Relative));
            detector = new Detector(sri.Stream.AsInputStream(), (uint)sri.Stream.Length);

            var previewSize = new Windows.Foundation.Size(1280, 720);
            cam = new Camera(previewSize, CameraSensorLocation.Front);
            im = new ImageProcessing(detector);

            cam.OnFrameReady += cam_FrameReady;
            im.frameProcessed += im_FrameProcessed;

            setupOK = SetupBluetoothLink();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (im != null)
            {
                im.frameProcessed -= im_FrameProcessed;
                im = null;
            }

            if (cam != null)
            {
                cam.OnFrameReady -= cam_FrameReady;
                cam.Dispose();
            }

            if (detector != null)
            {
                detector.Dispose();
            }

            if (s != null)
            {
                s.Dispose();
            }

            base.OnNavigatingFrom(e);
        }

        private void cam_FrameReady(uint width, uint height, uint dataPtr)
        {
            if (im != null)
            {
                im.processFrame(width, height, dataPtr);
                mvs = im.getMotorVelocities();

                string command = "CMD ";
                command += "M1=" + mvs[0] + ",";
                command += "M2=" + mvs[1] + '\n';

                sendCommand(command);
            }
        }

        private void im_FrameProcessed(uint width, uint height, uint dataPtr)
        {
            if (texInterop != null)
                texInterop.setTexturePtr(width, height, dataPtr);
        }

        private void canvas_Loaded(object sender, RoutedEventArgs e)
        {
            texInterop = new TextureGraphInterop();

            // Set window bounds in dips
            texInterop.WindowBounds = new Windows.Foundation.Size(
                (float)canvas.ActualWidth,
                (float)canvas.ActualHeight
                );

            // Set native resolution in pixels
            texInterop.NativeResolution = new Windows.Foundation.Size(
                (float)Math.Floor(canvas.ActualWidth * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f),
                (float)Math.Floor(canvas.ActualHeight * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f)
                );

            // Set render resolution to the full native resolution
            texInterop.RenderResolution = texInterop.NativeResolution;

            // Hook-up native component to DrawingSurface
            canvas.SetContentProvider(texInterop.CreateContentProvider());
            canvas.SetManipulationHandler(texInterop);
        }

        private async Task<bool> SetupBluetoothLink()
        {
            // Tell PeerFinder that we're a pair to anyone that has been paried with us over BT
            PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = "";

            // Find all peers
            var devices = await PeerFinder.FindAllPeersAsync();

            // If there are no peers, then complain
            if (devices.Count == 0)
            {
                MessageBox.Show("No bluetooth devices are paired, please pair your FoneAstra");

                // Neat little line to open the bluetooth settings
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return false;
            }

            // Convert peers to array from strange datatype return from PeerFinder.FindAllPeersAsync()
            PeerInformation[] peers = devices.ToArray();

            // Find paired peer that is the FoneAstra
            PeerInformation peerInfo = devices.FirstOrDefault(c => c.DisplayName.Contains("MCU-HC06"));

            // If that doesn't exist, complain!
            if (peerInfo == null)
            {
                MessageBox.Show("No paired FoneAstra was found, please pair your FoneAstra");
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return false;
            }

            // Otherwise, create our StreamSocket and connect it!
            s = new StreamSocket();
            s.Control.NoDelay = true;
            s.Control.OutboundBufferSizeInBytes = 20;
            await s.ConnectAsync(peerInfo.HostName, "1");

            output = new DataWriter(s.OutputStream);
            return true;
        }

        private async void sendCommand(String c)
        {
            try
            {
                output.WriteString(c);
                // Send the contents of the writer to the backing stream.
                await output.StoreAsync();
                // For the in-memory stream implementation we are using, the flushAsync call 
                // is superfluous,but other types of streams may require it.
                await output.FlushAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
            }
        }
    }
}
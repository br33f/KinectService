using KinectService.Control;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectService
{
    public partial class MainWindow : Window
    {
        private readonly string Database = @"D:\Dokumenty\PRACA_INŻYNIERSKA\SOFTWARE\Gesty\PullRightAndCross.gbd";

        private Kinect.Connection KinectConnection { get; set; }
        private Kinect.BodyReader KinectBR { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ContentRendered += OnContentRendered;
            Closing += OnWindowClosing;
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            InitializeKinectConnection();
            FillGestureList();
            InitializeApplicationService();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            this.KinectConnection.Dispose();
            this.KinectBR.Dispose();
            ApplicationService.Instance.Dispose();
        }


        private void InitializeApplicationService()
        {
            ApplicationService.Instance.Connected += OnApplicationServiceConnected;
            ApplicationService.Instance.Disconnected += OnApplicationServiceDisconnected;
            ApplicationService.Instance.StartListening();
        }

        private void InitializeKinectConnection()
        {
            this.KinectConnection = new Kinect.Connection(OnKinectAvailableChanged);
        }

        private void FillGestureList()
        {
            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.Database))
            {
                foreach (Gesture g in database.AvailableGestures)
                {
                    lvGestures.Items.Add(g.Name);
                }
            }
        }

        private void OnKinectAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (e.IsAvailable)
            {
                if (KinectBR == null)
                {
                    KinectBR = new Kinect.BodyReader(KinectConnection);

                    ShowConnected(kinectStatus);

                    ApplicationService.Instance.NotifyServiceUp();
                }
            } 
            else
            {
                KinectBR = null;

                ShowNotConnected(kinectStatus);

                ApplicationService.Instance.NotifyServiceDown();
            }
             
        }

        private void OnApplicationServiceConnected()
        {
            if (KinectBR == null)
            {
                ApplicationService.Instance.NotifyServiceDown();
            }
            else
            {
                ApplicationService.Instance.NotifyServiceUp();
            }

            ShowConnected(controlStatus);
        }

        private void OnApplicationServiceDisconnected()
        {
            ShowNotConnected(controlStatus);
        }

        private void ShowConnected(TextBlock textBlock)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                textBlock.Text = "Połączono";
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0, 220, 0));
            }));
        }

        private void ShowNotConnected(TextBlock textBlock)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                textBlock.Text = "Nie połączono";
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 0, 0));
            }));
        }

    }
}

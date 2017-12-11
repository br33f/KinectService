using KinectService.Control;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectService.Kinect
{
    class Detector : IDisposable
    {
        private Kinect.Connection KinectConnection { get; set; }

        // Confidence threshold
        private readonly float CONFIDENCE_THRESHOLD = 0.6f;

        // Gesture Recognition
        private VisualGestureBuilderFrameSource GestureFrameSource { get; set; }
        private VisualGestureBuilderFrameReader GestureFrameReader { get; set; }

        // Gesture database
        private readonly string Database = @"D:\Dokumenty\PRACA_INŻYNIERSKA\SOFTWARE\Gesty\PullRightAndCross.gbd";

        // Gestures Dictionary
        public Dictionary<String, Gesture> GesturesDictionary { get; set; }

        // Pull detection threshold
        private readonly float PULL_DETECTION_THRESHOLD = 0.15f;

        // Pull right
        private bool PullRightStarted = false;
        private float PullRightProgressAtStart;

        // Cross
        private readonly int CROSS_DETECTION_THRESHOLD = 15;
        private bool CrossDetected = false;
        private int CrossDetectedInRow = 0;

        public ulong TrackingID
        {
            get
            {
                return GestureFrameSource.TrackingId;
            } 
            set
            {
                if (GestureFrameSource.TrackingId != value)
                {
                    GestureFrameSource.TrackingId = value;
                    GestureFrameReader.IsPaused = GestureFrameSource.TrackingId == 0;
                }
            }
        }

        public Detector(Kinect.Connection kinectConnection)
        {
            this.GesturesDictionary = new Dictionary<string, Gesture>();

            this.KinectConnection = kinectConnection;
            this.InitializeGestureRecognition();
        }

        public void Dispose()
        {
            if (GestureFrameReader != null)
            {
                GestureFrameReader.FrameArrived -= OnGestureFrameArrived;
                GestureFrameReader.Dispose();
                GestureFrameReader = null;
            }

            if (GestureFrameSource != null)
            {
                GestureFrameSource.TrackingIdLost -= OnTrackingIdLost;
                GestureFrameSource.Dispose();
                GestureFrameSource = null;
            }
        }

        private void InitializeGestureRecognition()
        {
            GestureFrameSource = new VisualGestureBuilderFrameSource(KinectConnection.KSensor, 0);
            GestureFrameSource.TrackingIdLost += OnTrackingIdLost;

            if (GestureFrameSource != null)
            {
                GestureFrameReader = GestureFrameSource.OpenReader();
                GestureFrameReader.FrameArrived += OnGestureFrameArrived;
            }

            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.Database))
            {
                foreach (Gesture g in database.AvailableGestures)
                {
                    GestureFrameSource.AddGesture(g);
                    GesturesDictionary.Add(g.Name, g);
                }
            }
        }

        private void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            GestureFrameReader.IsPaused = true;
            Console.WriteLine("Lost tracking");
        }

        private void OnGestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;
                    IReadOnlyDictionary<Gesture, ContinuousGestureResult> continuousResults = frame.ContinuousGestureResults;
                    if (discreteResults != null)
                    {
                        // get results
                        DiscreteGestureResult pullRightResult = null;
                        discreteResults.TryGetValue(GesturesDictionary["Pull_Right"], out pullRightResult);

                        DiscreteGestureResult crossResult = null;
                        discreteResults.TryGetValue(GesturesDictionary["Cross"], out crossResult);


                        // Cross detection
                        if (crossResult.Detected && crossResult.Confidence > CONFIDENCE_THRESHOLD && !CrossDetected)
                        {
                            CrossDetected = true;
                            Console.WriteLine("Cross");
                            ApplicationService.Instance.Cross();
                            CrossDetectedInRow = CROSS_DETECTION_THRESHOLD;
                        } 
                        else if (!crossResult.Detected)
                        {
                            if (CrossDetectedInRow == 0)
                            {
                                CrossDetected = false;
                            } 
                            else
                            {
                                CrossDetectedInRow--;
                            }
                         }

                        // Pull down detection
                        if (pullRightResult.Detected && pullRightResult.Confidence > CONFIDENCE_THRESHOLD && PullRightStarted == false)
                        {
                            // On gesture start
                            if (continuousResults != null)
                            {
                                ContinuousGestureResult pullProgressRightResult = null;
                                continuousResults.TryGetValue(GesturesDictionary["PullProgress_Right"], out pullProgressRightResult);

                                PullRightStarted = true;
                                PullRightProgressAtStart = pullProgressRightResult.Progress;
                            }

                        }
                        else if (!pullRightResult.Detected && PullRightStarted == true)
                        {
                            // On gesture end
                            if (continuousResults != null)
                            {
                                ContinuousGestureResult pullProgressRightResult = null;
                                continuousResults.TryGetValue(GesturesDictionary["PullProgress_Right"], out pullProgressRightResult);

                                PullRightStarted = false;
                                float pullRightProgressAtEnd = pullProgressRightResult.Progress;

                                if (Math.Abs(pullRightProgressAtEnd - PullRightProgressAtStart) > PULL_DETECTION_THRESHOLD)
                                if (pullRightProgressAtEnd > PullRightProgressAtStart)
                                {
                                    Console.WriteLine("Pulled Up");
                                    ApplicationService.Instance.PullRight("up");
                                }
                                else
                                {
                                    Console.WriteLine("Pulled down");
                                    ApplicationService.Instance.PullRight("down");
                                }
                            }
                        }
                    }           
                }
            }
        }

    }
}

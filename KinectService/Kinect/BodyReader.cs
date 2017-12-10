using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectService.Kinect
{
    class BodyReader : IDisposable
    {
        private Kinect.Connection KinectConnection { get; set; }

        private BodyFrameSource BFSource { get; set; }
        private BodyFrameReader BFReader { get; set; }
        private Body[] Bodies { get; set; }
        private Detector[] Detectors { get; set; }

        public BodyReader(Kinect.Connection kinectConnection)
        {
            this.KinectConnection = kinectConnection;
            this.BFSource = KinectConnection.KSensor.BodyFrameSource;

            this.InitializeDetectors();
            this.InitializeBodyRecognition();
        }

        public void Dispose()
        {
            for (int id = 0; id < BFSource.BodyCount; id++)
            {
                Detectors[id].Dispose();
            }

            BFReader.FrameArrived -= OnBodyFrameArrived;
            BFReader.Dispose();
        }

        private void InitializeDetectors()
        {
            this.Detectors = new Detector[BFSource.BodyCount];
            for (int id = 0; id < BFSource.BodyCount; id++)
            {
                this.Detectors[id] = new Detector(KinectConnection);
            }
        }

        private void InitializeBodyRecognition()
        {   
            this.Bodies = new Body[BFSource.BodyCount];

            this.BFReader = BFSource.OpenReader();
            this.BFReader.FrameArrived += OnBodyFrameArrived;
        }

        private void OnBodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(Bodies);

                    for (int i = 0; i < BFSource.BodyCount; ++i)
                    {
                        Detectors[i].TrackingID = Bodies[i].TrackingId;
                    }
                }
            }
        }
      
    }
}

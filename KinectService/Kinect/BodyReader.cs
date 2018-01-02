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
        private GestureDetector[] GestureDetectors { get; set; }
        private JointDetector[] JointDetectors { get; set; }

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
                GestureDetectors[id].Dispose();
                JointDetectors[id].Dispose();
            }

            BFReader.FrameArrived -= OnBodyFrameArrived;
            BFReader.Dispose();
        }

        private void InitializeDetectors()
        {
            this.GestureDetectors = new GestureDetector[BFSource.BodyCount];
            for (int id = 0; id < BFSource.BodyCount; id++)
            {
                this.GestureDetectors[id] = new GestureDetector(KinectConnection);
            }

            this.JointDetectors = new JointDetector[BFSource.BodyCount];
            for (int id = 0; id < BFSource.BodyCount; id++)
            {
                this.JointDetectors[id] = new JointDetector();
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
                        GestureDetectors[i].TrackingID = Bodies[i].TrackingId;
                        JointDetectors[i].OnBodyUpdate(Bodies[i]);
                    }
                }
            }
        }
      
    }
}

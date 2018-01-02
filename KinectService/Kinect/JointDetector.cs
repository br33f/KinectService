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
    enum PullPosition {LEFT, RIGHT};

    struct SwipeParams
    {
        public bool grabbed;
        public CameraSpacePoint lastSavedPosition;
    }

    struct PullParams
    {
        public bool grabbed;
        public bool afterAction;
        public CameraSpacePoint lastSavedPosition;
    }

    class JointDetector : IDisposable
    {
        // Swipe
        private SwipeParams SwipeParams = new SwipeParams();
        private readonly float SIGNIFICANT_SWIPE_THRESHOLD = 0.1f;

        // Pull
        private PullParams PullParams = new PullParams();
        private readonly float PULL_ACTION_THRESHOLD = 0.12f;

        // Last updated body
        public Body LatestBody { get; set; }

        public void Dispose()
        {
        }

        public void OnBodyUpdate(Body body)
        {
            LatestBody = body;

            // Right hand
            if (body.HandRightState == HandState.Closed && body.HandRightConfidence == TrackingConfidence.High)
            {
                OnHandRightClose();
            }
            else if (body.HandRightState != HandState.Closed)
            {
                OnHandRightFree();
            }

            OnHandRightPositionChange();

            // Left hand
            if (body.HandLeftState == HandState.Closed && body.HandLeftConfidence == TrackingConfidence.High)
            {
                OnHandLeftClose();
            }
            else if (body.HandLeftState != HandState.Closed)
            {
                OnHandLeftFree();
            }

            OnHandLeftPositionChange();
        }

        private void OnHandRightClose()
        {
            if (SwipeParams.grabbed == false)
            {
                SwipeParams.grabbed = true;
                SwipeParams.lastSavedPosition = LatestBody.Joints[JointType.HandRight].Position;
            }
        }

        private void OnHandRightFree()
        {
            SwipeParams.grabbed = false;
        }

        private void OnHandRightPositionChange()
        {
            CameraSpacePoint handRightPosition = LatestBody.Joints[JointType.HandRight].Position;
            if (SwipeParams.grabbed)
            {
                float positionDiffrence = handRightPosition.X - SwipeParams.lastSavedPosition.X;
                if (Math.Abs(positionDiffrence) > SIGNIFICANT_SWIPE_THRESHOLD)
                {
                    SwipeParams.lastSavedPosition = handRightPosition;
                    ApplicationService.Instance.ChangeBrightness(positionDiffrence);
                }
            }
        }

        private void OnHandLeftClose()
        {
            if (PullParams.grabbed == false)
            {
                PullParams.grabbed = true;
                PullParams.lastSavedPosition = LatestBody.Joints[JointType.HandLeft].Position;
            }
        }

        private void OnHandLeftFree()
        {
            PullParams.grabbed = false;
            if (PullParams.afterAction)
            {
                PullParams.afterAction = false;
                ApplicationService.Instance.PullCancel();
                System.Diagnostics.Debug.WriteLine("PullCancel");
            }
        }

        private void OnHandLeftPositionChange()
        {
            CameraSpacePoint handLeftPosition = LatestBody.Joints[JointType.HandLeft].Position;
            if (PullParams.grabbed && !PullParams.afterAction)
            {
                float positionDiffrence = handLeftPosition.Y - PullParams.lastSavedPosition.Y;
                if (Math.Abs(positionDiffrence) > PULL_ACTION_THRESHOLD)
                {
                    PullParams.afterAction = true;
                    PullParams.lastSavedPosition = handLeftPosition;
                    if (positionDiffrence > 0)
                    {
                        PullUp();
                    }
                    else
                    {
                        PullDown();
                    }
                }
            }
        }

        private PullPosition GetPullPosition()
        {
            CameraSpacePoint handLeftPosition = LatestBody.Joints[JointType.HandLeft].Position;
            CameraSpacePoint shoulderLeftPosition = LatestBody.Joints[JointType.ShoulderLeft].Position;

            return handLeftPosition.X > shoulderLeftPosition.X ? PullPosition.RIGHT : PullPosition.LEFT;
        }

        private void PullUp()
        {
            if (GetPullPosition() == PullPosition.LEFT)
            {
                ApplicationService.Instance.PullLeft("up");
            }
            else
            {
                ApplicationService.Instance.PullRight("up");
            }

            System.Diagnostics.Debug.WriteLine("PullUp");
        }

        private void PullDown()
        {
            if (GetPullPosition() == PullPosition.LEFT)
            {
                ApplicationService.Instance.PullLeft("down");
            }
            else
            {
                ApplicationService.Instance.PullRight("down");
            }

            System.Diagnostics.Debug.WriteLine("PullDown");
        }

    }

}
